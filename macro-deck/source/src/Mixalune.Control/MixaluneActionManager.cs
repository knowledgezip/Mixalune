using System.Globalization;
using MacroDeck.Localization;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;

namespace Mixalune.Control;

public static class MixaluneActionManager
{
    public static IReadOnlyList<IActionDefinition> Create(MixaluneStateManager state, MixaluneConnectionManager connection) =>
    [
        new MixerAction("set-volume", "Set Channel Volume", "channel.setVolume", state, connection),
        new MixerAction("volume-up", "Volume Up", "channel.adjustVolume", state, connection),
        new MixerAction("volume-down", "Volume Down", "channel.adjustVolume", state, connection),
        new MixerAction("toggle-mute", "Toggle Channel Mute", "channel.toggleMute", state, connection),
        new MixerAction("set-mute", "Set Channel Mute", "channel.setMute", state, connection),
        new MixerAction("microphone-mute", "Toggle Microphone Mute", "microphone.toggleMute", state, connection),
        new MixerAction("reconnect", "Reconnect to Mixalune", "", state, connection)
    ];
}

public sealed class MixerAction(string id, string name, string command, MixaluneStateManager state, MixaluneConnectionManager connection)
    : IActionDefinition, IDynamicOptionsActionDefinition, IStateProviderActionDefinition
{
    public string Id => id;
    public LocalizedText Name => name;
    public LocalizedText Description => id == "reconnect" ? "Reconnect and synchronize with Mixalune." : "Control Mixalune and show its confirmed state.";
    public MacroDeckPlatform Platforms => MacroDeckPlatform.Windows;
    public IReadOnlyList<ActionParameter> Parameters
    {
        get
        {
            var parameters = new List<ActionParameter>();
            if (id is not ("reconnect" or "microphone-mute"))
                parameters.Add(ActionParameter.DynamicChoice("channelId", label: "Channel", required: true));
            if (id is "set-volume" or "volume-up" or "volume-down")
            {
                parameters.Add(ActionParameter.Choice("bus",
                    [new() { Value = "game", Label = "Game / stream" }, new() { Value = "monitor", Label = "Monitor" }],
                    label: "Output fader", defaultValue: "game", required: true));
                parameters.Add(ActionParameter.Slider("percent", 0, 100, label: id == "set-volume" ? "Volume (%)" : "Increment (%)", defaultValue: id == "set-volume" ? 50 : 5));
            }
            if (id == "set-mute")
                parameters.Add(ActionParameter.Choice("muted", [new() { Value = "true", Label = "On" }, new() { Value = "false", Label = "Off" }],
                    label: "Mute", defaultValue: "true", required: true));
            return parameters;
        }
    }
    public Task<DynamicOptionsResult> GetDynamicOptionsAsync(DynamicOptionsContext context, CancellationToken cancellationToken)
    {
        var snapshot = state.Snapshot;
        var options = snapshot.Channels.Values.Select(c => new ActionParameterOption
        { Value = c.Id, Label = snapshot.Connected ? c.Name : $"{c.Name} (disconnected)" }).ToList();
        if (context.CurrentParameters.GetValueOrDefault("channelId") is string selected && !snapshot.Channels.ContainsKey(selected))
            options.Add(new() { Value = selected, Label = $"{selected} (unavailable — select a replacement)" });
        return Task.FromResult(new DynamicOptionsResult { Options = options });
    }
    public TimeSpan StatePollInterval => TimeSpan.FromSeconds(1);
    public Task<ActionStateSnapshot?> GetActionStateAsync(IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        var snapshot = state.Snapshot;
        var channelId = id == "microphone-mute" ? snapshot.MicrophoneId : parameters.GetValueOrDefault("channelId")?.ToString();
        snapshot.Channels.TryGetValue(channelId ?? "", out var channel);
        var supported = snapshot.Commands.Contains(command) ||
            (id == "microphone-mute" && snapshot.Commands.Contains("channel.toggleMute"));
        var available = snapshot.Connected && (id == "reconnect" || (channel is not null && supported));
        if (id == "reconnect")
            return Task.FromResult<ActionStateSnapshot?>(new(
                [new("connected", "Connected"), new("unavailable", "Disconnected")], available ? "connected" : "unavailable"));
        var active = !available ? "unavailable" : channel?.Muted == true ? "muted" : "unmuted";
        IReadOnlyList<ActionStateDefinition> states =
        [
            new("unmuted", "Unmuted") { DefaultAppearance = new() { BackgroundColor = "#246b68" } },
            new("muted", "Muted") { DefaultAppearance = new() { BackgroundColor = "#923f60" } },
            new("unavailable", "Unavailable") { DefaultAppearance = new() { BackgroundColor = "#444452" } }
        ];
        return Task.FromResult<ActionStateSnapshot?>(new(states, active));
    }
    public IActionExecutor CreateExecutor() => new Executor(this);
    sealed class Executor(MixerAction action) : IActionExecutor
    {
        public Task<ActionResult> ExecuteAsync(ActionExecutionContext context) => action.ExecuteAsync(context.Parameters.ToDictionary(x => x.Key, x => (object?)x.Value), context.CancellationToken);
    }
    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        try
        {
            if (id == "reconnect")
            {
                await connection.ReconnectAsync();
                return ActionResult.Accepted("Reconnecting; the connection variable reports when synchronization completes.");
            }
            var snapshot = state.Snapshot;
            if (!snapshot.Connected) return ActionResult.Failed(ActionErrorCodes.NotConnected, "Mixalune is disconnected. Enable its local API and check connection settings.");
            var channelId = id == "microphone-mute" ? snapshot.MicrophoneId : parameters.GetValueOrDefault("channelId")?.ToString();
            if (channelId is null || !snapshot.Channels.ContainsKey(channelId))
                return ActionResult.Failed(ActionErrorCodes.InvalidParameter, "The saved channel is unavailable. Choose a replacement.");
            var selectedCommand = command;
            var args = new Dictionary<string, object>();
            if (id != "microphone-mute") args["channelId"] = channelId;
            else if (!snapshot.Commands.Contains(selectedCommand)) { selectedCommand = "channel.toggleMute"; args["channelId"] = channelId; }
            if (id is "set-volume" or "volume-up" or "volume-down")
            {
                var bus = parameters.GetValueOrDefault("bus")?.ToString() ?? "game";
                if (bus is not ("game" or "monitor")) return ActionResult.Failed(ActionErrorCodes.InvalidParameter, "Choose a game or monitor fader.");
                args["bus"] = bus;
                if (!double.TryParse(parameters.GetValueOrDefault("percent")?.ToString() ?? (id == "set-volume" ? "" : "5"), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ||
                    !double.IsFinite(percent) || percent < 0 || percent > 100)
                    return ActionResult.Failed(ActionErrorCodes.InvalidParameter, "Enter a percentage from 0 to 100.");
                args[id == "set-volume" ? "volume" : "delta"] = percent / 100 * (id == "volume-down" ? -1 : 1);
            }
            if (id == "set-mute")
            {
                if (!bool.TryParse(parameters.GetValueOrDefault("muted")?.ToString(), out var muted))
                    return ActionResult.Failed(ActionErrorCodes.InvalidParameter, "Choose On or Off.");
                args["muted"] = muted;
            }
            var result = await connection.ExecuteAsync(selectedCommand, args, cancellationToken);
            return ActionResult.Success(result.GetProperty("muted").GetBoolean() ? "muted" : "unmuted");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { return ActionResult.Failed("mixalune-unavailable", ex is MixaluneApiException api ? api.Message : "The connection was interrupted. No command was retried."); }
    }
}
