using System.Text;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.ConfigFlow;
using MacroDeck.Sdk.Variables;

namespace Mixalune.Control;

public sealed class MixaluneIntegration : IPluginIntegration, IConfigFlowProvider, IVariableProvider
{
    readonly MixaluneStateManager state;
    readonly MixaluneConnectionManager connection;
    IIntegrationContext? context;
    string channelSignature = "";
    int notificationPending;
    int notificationVersion;
    public MixaluneIntegration(MixaluneStateManager state, MixaluneConnectionManager connection)
    {
        this.state = state;
        this.connection = connection;
        Actions = MixaluneActionManager.Create(state, connection);
    }
    public IReadOnlyList<IActionDefinition> Actions { get; }
    public bool AllowsMultipleConfigurations => false;
    public IConfigFlow CreateConfigFlow() => new MixaluneConfigFlow(state);
    public async Task InitializeAsync(IIntegrationContext context)
    {
        state.Changed -= OnChanged;
        this.context = context;
        state.Changed += OnChanged;
        try { await connection.ConfigureAsync(await MixaluneSettingsManager.LoadAsync(context)); }
        catch
        {
            await connection.ConfigureAsync(null);
            state.Status("Not configured", "Open Mixalune Control configuration and enter valid connection settings.");
        }
    }
    public async Task ShutdownAsync()
    {
        state.Changed -= OnChanged;
        await connection.DisposeAsync();
        context = null;
    }
    void OnChanged()
    {
        Interlocked.Increment(ref notificationVersion);
        ScheduleNotification();
    }
    void ScheduleNotification()
    {
        if (Interlocked.Exchange(ref notificationPending, 1) == 0) _ = NotifyAsync();
    }
    async Task NotifyAsync()
    {
        var observedVersion = 0;
        try
        {
            await Task.Delay(75);
            observedVersion = Volatile.Read(ref notificationVersion);
            var host = context;
            if (host is null) return;
            var signature = string.Join("|", state.Snapshot.Channels.Keys.Order(StringComparer.Ordinal));
            if (signature != channelSignature)
            {
                channelSignature = signature;
                await host.Variables.NotifyDeclarationsChangedAsync();
            }
            await host.Variables.RequestRefreshAsync();
        }
        catch { /* Host may be shutting down; subsequent reads still use the cache. */ }
        finally
        {
            Interlocked.Exchange(ref notificationPending, 0);
            // A channel may be added while awaiting the host. Do not lose its declaration
            // notification when coalescing a burst of volume events.
            if (context is not null && observedVersion != Volatile.Read(ref notificationVersion)) ScheduleNotification();
        }
    }
    public static string ChannelKey(string id)
    {
        var bytes = Encoding.UTF8.GetBytes(id);
        return bytes.Length <= 20 ? Convert.ToHexStringLower(bytes)
            : "h" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes).AsSpan(0, 16));
    }
    static VariableDefinition Variable(string id, VariableType type) =>
        VariableDefinition.Eager("mixalune_" + id.Replace('-', '_'), type, refreshInterval: TimeSpan.FromSeconds(1)) with { Id = id };
    public IReadOnlyList<VariableDefinition> Variables
    {
        get
        {
            var variables = new List<VariableDefinition>
            {
                Variable("connected", VariableType.Boolean), Variable("status", VariableType.Text),
                Variable("edition", VariableType.Text), Variable("version", VariableType.Text),
                Variable("api-version", VariableType.Text), Variable("channel-count", VariableType.Numeric),
                Variable("last-error", VariableType.Text), Variable("microphone-muted", VariableType.Boolean)
            };
            foreach (var channel in state.Snapshot.Channels.Values)
            {
                var prefix = "channel-" + ChannelKey(channel.Id);
                variables.Add(Variable(prefix + "-volume", VariableType.Numeric));
                variables.Add(Variable(prefix + "-monitor-volume", VariableType.Numeric));
                variables.Add(Variable(prefix + "-muted", VariableType.Boolean));
            }
            return variables;
        }
    }
    public ValueTask<VariableReading> ReadAsync(string localId, CancellationToken cancellationToken = default)
    {
        var s = state.Snapshot;
        VariableReading reading = localId switch
        {
            "connected" => VariableReading.Of(s.Connected),
            "status" => VariableReading.Of(s.Status),
            "last-error" => VariableReading.Of(s.Error ?? ""),
            _ when !s.Connected => VariableReading.Unavailable,
            "edition" => VariableReading.Of(s.Edition ?? "Not reported"),
            "version" when s.Version is not null => VariableReading.Of(s.Version),
            "api-version" => VariableReading.Of("1.0"),
            "channel-count" => VariableReading.Of(s.Channels.Count),
            "microphone-muted" when s.MicrophoneId is not null && s.Channels.TryGetValue(s.MicrophoneId, out var microphone) => VariableReading.Of(microphone.Muted),
            _ => ReadChannel(s, localId)
        };
        return ValueTask.FromResult(reading);
    }
    static VariableReading ReadChannel(MixaluneSnapshot snapshot, string id)
    {
        foreach (var channel in snapshot.Channels.Values)
        {
            var prefix = "channel-" + ChannelKey(channel.Id);
            if (id == prefix + "-volume") return VariableReading.Of(Math.Round(channel.GameVolume * 100, 1));
            if (id == prefix + "-monitor-volume") return VariableReading.Of(Math.Round(channel.MonitorVolume * 100, 1));
            if (id == prefix + "-muted") return VariableReading.Of(channel.Muted);
        }
        return VariableReading.Unavailable;
    }
}
