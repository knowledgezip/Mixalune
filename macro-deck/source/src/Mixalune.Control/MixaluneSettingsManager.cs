using System.Globalization;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.ConfigFlow;

namespace Mixalune.Control;

public static class MixaluneSettingsManager
{
    public static async Task<ConnectionSettings?> LoadAsync(IIntegrationContext context)
    {
        var entries = await context.Config.GetEntriesAsync();
        if (entries.Count == 0) return null;
        var id = entries[0].Id;
        var token = await context.Config.GetSecretAsync(id, "token");
        var port = await context.Config.GetStringAsync(id, "port");
        var connectTimeout = await context.Config.GetStringAsync(id, "connectionTimeout");
        var requestTimeout = await context.Config.GetStringAsync(id, "requestTimeout");
        if (string.IsNullOrEmpty(token)) return null;
        return new(Parse(port, 18765, 1024, 65535), token, Parse(connectTimeout, 5, 1, 60), Parse(requestTimeout, 5, 1, 60));
    }
    internal static int Parse(object? value, int fallback, int minimum, int maximum)
    {
        if (value is null) return fallback;
        if (!int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) || result < minimum || result > maximum)
            throw new MixaluneApiException("CONFIGURATION", $"Enter a whole number between {minimum} and {maximum}.");
        return result;
    }
}

public sealed class MixaluneConfigFlow(MixaluneStateManager state) : IConfigFlow
{
    ConfigFlowStep Step() => new()
    {
        StepId = "connection",
        Title = "Connect to Mixalune",
        Description = $"Host: 127.0.0.1 (this computer). Status: {state.Snapshot.Status}. " +
            $"Mixalune: {state.Snapshot.Version ?? "unknown"}; API: 1.0 required; edition: {state.Snapshot.Edition ?? "not reported"}; " +
            $"channels: {state.Snapshot.Channels.Count}. {state.Snapshot.Error} " +
            "Submitting tests authentication before saving. Enable Mixalune's local API and copy its token.",
        Fields =
        [
            ActionParameter.Text("port", label: "Port", defaultValue: "18765", required: true),
            ActionParameter.Secret("token", label: "Authentication token", required: true),
            ActionParameter.Text("connectionTimeout", label: "Connection timeout (seconds)", defaultValue: "5", required: true),
            ActionParameter.Text("requestTimeout", label: "Request timeout (seconds)", defaultValue: "5", required: true)
        ]
    };
    public Task<ConfigFlowResult> StartAsync(IConfigFlowContext context, CancellationToken cancellationToken) =>
        Task.FromResult(ConfigFlowResult.Step(Step()));
    public async Task<ConfigFlowResult> SubmitAsync(string stepId, IReadOnlyDictionary<string, object?> input, IConfigFlowContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (stepId != "connection") return ConfigFlowResult.Error(Step(), "Reopen the connection setup.");
            var port = MixaluneSettingsManager.Parse(input.GetValueOrDefault("port"), 18765, 1024, 65535);
            var timeout = MixaluneSettingsManager.Parse(input.GetValueOrDefault("connectionTimeout"), 5, 1, 60);
            var request = MixaluneSettingsManager.Parse(input.GetValueOrDefault("requestTimeout"), 5, 1, 60);
            var token = input.GetValueOrDefault("token")?.ToString() ?? "";
            await using var client = new MixaluneApiClient(TimeSpan.FromSeconds(request));
            await client.ConnectAsync(port, token, TimeSpan.FromSeconds(timeout), cancellationToken);
            var version = await client.CommandAsync("system.getVersion", new { }, cancellationToken);
            if (version.GetProperty("apiVersion").GetString() != "1.0") throw new MixaluneApiException("VERSION", "Local API 1.0 is required.");
            await client.CommandAsync("system.getCapabilities", new { }, cancellationToken);
            return ConfigFlowResult.Complete("Mixalune on this computer", new Dictionary<string, ConfigFlowValue>
            {
                ["port"] = ConfigFlowValue.Plain(port.ToString(CultureInfo.InvariantCulture)),
                ["token"] = ConfigFlowValue.Secret(token),
                ["connectionTimeout"] = ConfigFlowValue.Plain(timeout.ToString(CultureInfo.InvariantCulture)),
                ["requestTimeout"] = ConfigFlowValue.Plain(request.ToString(CultureInfo.InvariantCulture))
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            return ConfigFlowResult.Error(Step(), ex is MixaluneApiException api ? api.Message : "Connection test failed. Enable the local API in Mixalune and check the port.");
        }
    }
}
