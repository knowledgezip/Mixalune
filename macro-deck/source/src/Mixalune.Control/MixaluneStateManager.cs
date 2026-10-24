using System.Text.Json;

namespace Mixalune.Control;

public sealed record MixaluneChannel(string Id, string Name, double GameVolume, double MonitorVolume, bool Muted);
public sealed record MixaluneSnapshot(bool Connected, string Status, string? Version, string? Edition,
    IReadOnlyDictionary<string, MixaluneChannel> Channels, IReadOnlySet<string> Commands, string? MicrophoneId, string? Error);

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
            Commands = commands, Version = version.GetProperty("version").GetString(),
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
