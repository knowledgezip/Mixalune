using System.Text.Json;

namespace Mixalune.Control;

public sealed record ConnectionSettings(int Port, string Token, int ConnectionTimeoutSeconds = 5, int RequestTimeoutSeconds = 5);

public sealed class MixaluneConnectionManager(MixaluneStateManager state) : IAsyncDisposable
{
    readonly SemaphoreSlim lifecycle = new(1);
    CancellationTokenSource? cancellation;
    Task? worker;
    MixaluneApiClient? active;
    ConnectionSettings? settings;
    public async Task ConfigureAsync(ConnectionSettings? configuration)
    {
        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
            settings = configuration;
            if (configuration is null) { state.Status("Not configured"); return; }
            cancellation = new();
            worker = RunAsync(configuration, cancellation.Token);
        }
        finally { lifecycle.Release(); }
    }
    public Task ReconnectAsync() => settings is null
        ? Task.FromException(new MixaluneApiException("CONFIGURATION", "Configure Mixalune Control before reconnecting."))
        : ConfigureAsync(settings);
    async Task StopCoreAsync()
    {
        cancellation?.Cancel();
        active?.Abort();
        if (worker is not null) { try { await worker.ConfigureAwait(false); } catch { } }
        worker = null;
        active = null;
        cancellation?.Dispose();
        cancellation = null;
        state.Status("Disconnected");
    }
    async Task RunAsync(ConnectionSettings configuration, CancellationToken stop)
    {
        var failures = 0;
        while (!stop.IsCancellationRequested)
        {
            state.Status("Connecting");
            await using var client = new MixaluneApiClient(TimeSpan.FromSeconds(configuration.RequestTimeoutSeconds));
            client.EventReceived += state.OnEvent;
            client.ResponseReceived += state.OnResponse;
            try
            {
                await client.ConnectAsync(configuration.Port, configuration.Token, TimeSpan.FromSeconds(configuration.ConnectionTimeoutSeconds), stop).ConfigureAwait(false);
                // Pause unsolicited events while taking snapshots; subscribe then take a final snapshot.
                await client.CommandAsync("events.subscribe", new { categories = Array.Empty<string>() }, stop).ConfigureAwait(false);
                var capabilities = await client.CommandAsync("system.getCapabilities", new { }, stop).ConfigureAwait(false);
                var version = await client.CommandAsync("system.getVersion", new { }, stop).ConfigureAwait(false);
                state.Metadata(capabilities, version);
                await client.CommandAsync("channel.list", new { }, stop).ConfigureAwait(false);
                var categories = capabilities.GetProperty("eventCategories").EnumerateArray().Select(x => x.GetString())
                    .Where(x => x is "channel" or "microphone" or "system" or "effects").ToArray();
                await client.CommandAsync("events.subscribe", new { categories }, stop).ConfigureAwait(false);
                await client.CommandAsync("system.getState", new { }, stop).ConfigureAwait(false);
                active = client;
                state.Status("Connected");
                failures = 0;
                while (!stop.IsCancellationRequested)
                {
                    var delay = Task.Delay(TimeSpan.FromSeconds(15), stop);
                    if (await Task.WhenAny(client.Completion, delay).ConfigureAwait(false) == client.Completion)
                    {
                        await client.Completion.ConfigureAwait(false);
                        throw new MixaluneApiException("DISCONNECTED", "Mixalune disconnected. Waiting to reconnect.");
                    }
                    await delay.ConfigureAwait(false);
                    if (state.Snapshot.Commands.Contains("system.ping"))
                        await client.CommandAsync("system.ping", new { }, stop).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                state.Status("Disconnected", ex is MixaluneApiException api ? api.Message : "Cannot reach Mixalune. Check that its local API is enabled and the port matches.");
            }
            finally
            {
                active = null;
                client.EventReceived -= state.OnEvent;
                client.ResponseReceived -= state.OnResponse;
                client.Abort();
            }
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(failures++, 5)))) + TimeSpan.FromMilliseconds(Random.Shared.Next(250)), stop).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }
    public async Task<JsonElement> ExecuteAsync(string command, object parameters, CancellationToken cancellationToken)
    {
        var client = active;
        var snapshot = state.Snapshot;
        if (client is null || !snapshot.Connected) throw new MixaluneApiException("DISCONNECTED", "Mixalune is disconnected. Enable its local API or use Reconnect to Mixalune.");
        if (!snapshot.Commands.Contains(command)) throw new MixaluneApiException("UNSUPPORTED_FEATURE", MixaluneApiClient.FriendlyError("UNSUPPORTED_FEATURE"));
        return await client.CommandAsync(command, parameters, cancellationToken).ConfigureAwait(false);
    }
    public async ValueTask DisposeAsync() => await ConfigureAsync(null).ConfigureAwait(false);
}
