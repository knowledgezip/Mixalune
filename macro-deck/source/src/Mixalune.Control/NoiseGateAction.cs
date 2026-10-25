using System.Globalization;
using MacroDeck.Localization;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;

namespace Mixalune.Control;

public sealed class NoiseGateAction(string id, string name, MixaluneStateManager state, MixaluneConnectionManager connection)
    : IActionDefinition, IStateProviderActionDefinition
{
    bool Absolute => id == "noise-gate-set";
    string Command => Absolute ? "effects.setThreshold" : "effects.adjustThreshold";
    public string Id => id;
    public LocalizedText Name => name;
    public LocalizedText Description => "Adjust the microphone noise gate threshold in dB. Higher values reject louder background sounds. Does not enable the gate.";
    public MacroDeckPlatform Platforms => MacroDeckPlatform.Windows;
    public IReadOnlyList<ActionParameter> Parameters =>
        [ActionParameter.Slider("decibels", Absolute ? -80 : 1, Absolute ? -10 : 70,
            label: Absolute ? "Threshold (dB)" : "Increment (dB)", defaultValue: Absolute ? -45 : 1)];
    public TimeSpan StatePollInterval => TimeSpan.FromSeconds(1);
    public Task<ActionStateSnapshot?> GetActionStateAsync(IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        var s = state.Snapshot;
        var active = !s.Connected || s.NoiseGate is null || !s.Commands.Contains(Command) ? "unavailable" : s.NoiseGate.Enabled ? "enabled" : "disabled";
        return Task.FromResult<ActionStateSnapshot?>(new(
            [new("enabled", "Gate enabled"), new("disabled", "Gate disabled"), new("unavailable", "Unavailable")], active));
    }
    public IActionExecutor CreateExecutor() => new Executor(this);
    sealed class Executor(NoiseGateAction action) : IActionExecutor
    {
        public Task<ActionResult> ExecuteAsync(ActionExecutionContext context) => action.ExecuteAsync(context.Parameters.ToDictionary(x => x.Key, x => (object?)x.Value), context.CancellationToken);
    }
    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        var s = state.Snapshot;
        if (!s.Connected) return ActionResult.Failed(ActionErrorCodes.NotConnected, "Connect to Mixalune first.");
        if (!s.Commands.Contains(Command)) return ActionResult.Failed("unsupported-feature", "This Mixalune build does not offer noise-gate threshold control. Update Mixalune or check its available features.");
        if (s.NoiseGate is null) return ActionResult.Failed("unsupported-feature", "The microphone noise gate is unavailable.");
        if (!double.TryParse(Convert.ToString(parameters.GetValueOrDefault("decibels"), CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !double.IsFinite(value) || value < (Absolute ? -80 : 1) || value > (Absolute ? -10 : 70))
            return ActionResult.Failed(ActionErrorCodes.InvalidParameter, Absolute ? "Enter a threshold from -80 to -10 dB." : "Enter an increment from 1 to 70 dB.");
        try
        {
            var args = new Dictionary<string, object> { ["effectId"] = "noise-gate", [Absolute ? "thresholdDb" : "deltaDb"] = value * (id == "noise-gate-down" ? -1 : 1) };
            var result = await connection.ExecuteAsync(Command, args, cancellationToken);
            return ActionResult.Success(result.GetProperty("enabled").GetBoolean() ? "enabled" : "disabled");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { return ActionResult.Failed("mixalune-unavailable", ex is MixaluneApiException api ? api.Message : "The connection was interrupted. No command was retried."); }
    }
}
