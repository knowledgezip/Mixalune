using System.Text.Json;

namespace Mixalune.Control;

public sealed record MixaluneChannel(string Id, string Name, double GameVolume, double MonitorVolume, bool Muted);
public sealed record MixaluneNoiseGate(bool Enabled, double ThresholdDb);
public sealed record MixaluneSnapshot(bool Connected, string Status, string? Version, string? Edition,
    IReadOnlyDictionary<string, MixaluneChannel> Channels, IReadOnlySet<string> Commands, string? MicrophoneId, string? Error, MixaluneNoiseGate? NoiseGate = null);

public sealed class MixaluneStateManager
{
    readonly object gate = new();
    MixaluneSnapshot snapshot = new(false, "Disconnected", null, null,
        new Dictionary<string, MixaluneChannel>(), new HashSet<string>(), null, null);
    public MixaluneSnapshot Snapshot { get { lock (gate) return snapshot; } }
    public event Action? Changed;
    public void Status(string status, string? error = null)
    {
        lock (gate) snapshot = snapshot with { Connected = status == "Connected", Status = status, Error = error };
        Changed?.Invoke();
    }
    public void Metadata(JsonElement capabilities, JsonElement version)
    {
        var commands = capabilities.GetProperty("commands").EnumerateArray().Select(x => x.GetString() ?? throw new JsonException()).ToHashSet(StringComparer.Ordinal);
        if (version.GetProperty("apiVersion").GetString() != "1.0") throw new MixaluneApiException("VERSION", "This plugin requires local API 1.0.");
        lock (gate) snapshot = snapshot with
        {
            Commands = commands, Version = version.GetProperty("version").GetString(), NoiseGate = null,
            Edition = capabilities.TryGetProperty("edition", out var edition) && edition.ValueKind == JsonValueKind.String ? edition.GetString() : null
        };
        Changed?.Invoke();
    }
    public void ReplaceChannels(JsonElement data)
    {
        var channels = data.GetProperty("channels").EnumerateArray().Select(Parse).ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (channels.Count > 512) throw new JsonException("Too many channels.");
        lock (gate)
        {
            snapshot = snapshot with { Channels = channels };
            if (data.TryGetProperty("microphone", out var mic)) snapshot = snapshot with { MicrophoneId = Parse(mic).Id };
            if (data.TryGetProperty("effects", out var effects)) snapshot = snapshot with { NoiseGate = ParseEffects(effects) };
        }
        Changed?.Invoke();
    }
    public void ConfirmChannel(JsonElement data, bool microphone = false)
    {
        var channel = Parse(data);
        lock (gate)
        {
            var channels = snapshot.Channels.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            channels[channel.Id] = channel;
            snapshot = snapshot with { Channels = channels, MicrophoneId = microphone ? channel.Id : snapshot.MicrophoneId };
        }
        Changed?.Invoke();
    }
    public void OnEvent(JsonElement message)
    {
        var data = message.GetProperty("data");
        switch (message.GetProperty("event").GetString())
        {
            case "system.stateChanged": ReplaceChannels(data); break;
            case "effects.changed": ReplaceEffects(data); break;
            case "channel.volumeChanged":
            case "channel.muteChanged":
            case "channel.added": ConfirmChannel(data); break;
            case "microphone.stateChanged": ConfirmChannel(data, true); break;
            case "channel.removed":
                lock (gate)
                {
                    var channels = snapshot.Channels.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                    channels.Remove(data.GetProperty("channelId").GetString()!);
                    snapshot = snapshot with { Channels = channels };
                }
                Changed?.Invoke();
                break;
        }
    }
    public void OnResponse(string command, JsonElement data)
    {
        // Apply responses on the receive loop before reading the next event. A delayed caller
        // continuation must never overwrite a newer event with an older command response.
        if (command is "channel.list" or "system.getState") ReplaceChannels(data);
        else if (command.StartsWith("channel.", StringComparison.Ordinal)) ConfirmChannel(data);
        else if (command.StartsWith("microphone.", StringComparison.Ordinal)) ConfirmChannel(data, true);
        else if (command == "effects.list") ReplaceEffects(data);
        else if (command.StartsWith("effects.", StringComparison.Ordinal) && data.GetProperty("effectId").GetString() == "noise-gate")
        {
            var value = ParseGate(data);
            lock (gate) snapshot = snapshot with { NoiseGate = value };
            Changed?.Invoke();
        }
    }
    void ReplaceEffects(JsonElement data)
    {
        var value = ParseEffects(data);
        lock (gate) snapshot = snapshot with { NoiseGate = value };
        Changed?.Invoke();
    }
    static MixaluneNoiseGate? ParseEffects(JsonElement data)
    {
        var effects = data.GetProperty("effects");
        if (effects.GetArrayLength() > 512) throw new JsonException("Too many effects.");
        MixaluneNoiseGate? result = null;
        foreach (var effect in effects.EnumerateArray())
            if (effect.GetProperty("effectId").GetString() == "noise-gate")
            {
                if (result is not null) throw new JsonException("Duplicate noise gate.");
                result = ParseGate(effect);
            }
        return result;
    }
    static MixaluneNoiseGate ParseGate(JsonElement data)
    {
        var threshold = data.GetProperty("thresholdDb").GetDouble();
        if (!double.IsFinite(threshold) || threshold < -80 || threshold > -10) throw new JsonException("Invalid noise gate threshold.");
        return new(data.GetProperty("enabled").GetBoolean(), threshold);
    }
    static MixaluneChannel Parse(JsonElement data)
    {
        var id = data.GetProperty("channelId").GetString();
        var name = data.GetProperty("name").GetString();
        var game = data.GetProperty("gameVolume").GetDouble();
        var monitor = data.GetProperty("monitorVolume").GetDouble();
        if (string.IsNullOrEmpty(id) || id.Length > 256 || name is null || name.Length > 1024 ||
            !double.IsFinite(game) || game < 0 || game > 1 || !double.IsFinite(monitor) || monitor < 0 || monitor > 1) throw new JsonException("Invalid channel.");
        return new(id, name, game, monitor, data.GetProperty("muted").GetBoolean());
    }
}
