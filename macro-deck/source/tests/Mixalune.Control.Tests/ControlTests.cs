using MacroDeck.Plugin.Testing;
using MacroDeck.Sdk.Actions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;

namespace Mixalune.Control.Tests;

[TestFixture]
public sealed class ControlTests
{
    MockMixalune server = null!;
    MixaluneStateManager state = null!;
    MixaluneConnectionManager manager = null!;

    [SetUp]
    public async Task Setup()
    {
        server = new();
        await server.StartAsync();
        state = new();
        manager = new(state);
    }
    [TearDown]
    public async Task TearDown() { await manager.DisposeAsync(); await server.DisposeAsync(); }
    async Task Connect()
    {
        await manager.ConfigureAsync(new(server.Port, MockMixalune.Token, 2, 1));
        await Until(() => state.Snapshot.Connected);
    }
    static async Task Until(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!predicate()) await Task.Delay(20, timeout.Token);
    }
    MixerAction Action(string id) => (MixerAction)MixaluneActionManager.Create(state, manager).Single(x => x.Id == id);
    static Dictionary<string, object?> Parameters(double percent = 5) => new() { ["channelId"] = "custom-mic", ["percent"] = percent };

    [Test]
    public async Task Authenticates_and_discovers_capabilities_channels_and_microphone()
    {
        await Connect();
        Assert.That(state.Snapshot.Channels["custom-mic"].Name, Is.EqualTo(server.ChannelName));
        Assert.That(state.Snapshot.MicrophoneId, Is.EqualTo("custom-mic"));
        Assert.That(state.Snapshot.Edition, Is.Null);
        Assert.That(state.Snapshot.Commands, Does.Contain("channel.setVolume"));
    }
    [Test]
    public async Task Invalid_token_is_rejected_without_echoing_server_text()
    {
        await using var client = new MixaluneApiClient(TimeSpan.FromSeconds(1));
        var error = Assert.ThrowsAsync<MixaluneApiException>(() => client.ConnectAsync(server.Port, new string('x', 40), TimeSpan.FromSeconds(2), default));
        Assert.That(error!.Code, Is.EqualTo("UNAUTHORIZED"));
        Assert.That(error.Message, Does.Not.Contain(MockMixalune.Token));
    }
    [Test]
    public async Task Missing_server_or_wrong_port_leaves_state_disconnected()
    {
        var port = server.Port;
        await server.DisposeAsync();
        await manager.ConfigureAsync(new(port, MockMixalune.Token, 1, 1));
        await Until(() => state.Snapshot.Error is not null);
        Assert.That(state.Snapshot.Connected, Is.False);
    }
    [Test]
    public async Task Incompatible_api_version_is_rejected()
    {
        server.ApiVersion = "2.0";
        await using var client = new MixaluneApiClient(TimeSpan.FromSeconds(1));
        var error = Assert.ThrowsAsync<MixaluneApiException>(async () => await client.ConnectAsync(server.Port, MockMixalune.Token, TimeSpan.FromSeconds(2), default));
        Assert.That(error!.Code, Is.EqualTo("VERSION"));
    }
    [TestCase("set-volume", 75, .75)]
    [TestCase("volume-up", 5, .55)]
    [TestCase("volume-down", 5, .45)]
    [TestCase("volume-up", 100, 1)]
    [TestCase("volume-down", 100, 0)]
    public async Task Volume_controls_and_boundaries(string action, double percent, double expected)
    {
        await Connect();
        Assert.That((await Action(action).ExecuteAsync(Parameters(percent), default)).Status, Is.EqualTo(ActionResultStatus.Succeeded));
        Assert.That(state.Snapshot.Channels["custom-mic"].GameVolume, Is.EqualTo(expected).Within(.0001));
    }
    [TestCase(-1)]
    [TestCase(101)]
    [TestCase(double.NaN)]
    public async Task Invalid_percent_never_sends_command(double percent)
    {
        await Connect();
        Assert.That((await Action("set-volume").ExecuteAsync(Parameters(percent), default)).Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(server.Commands, Does.Not.Contain("channel.setVolume"));
    }
    [Test]
    public async Task Monitor_bus_is_independent()
    {
        await Connect();
        var parameters = Parameters(90); parameters["bus"] = "monitor";
        await Action("set-volume").ExecuteAsync(parameters, default);
        Assert.That(state.Snapshot.Channels["custom-mic"].MonitorVolume, Is.EqualTo(.9));
        Assert.That(state.Snapshot.Channels["custom-mic"].GameVolume, Is.EqualTo(.5));
    }
    [TestCase("toggle-mute")]
    [TestCase("microphone-mute")]
    public async Task Toggle_mute_is_confirmed(string action)
    {
        await Connect();
        Assert.That((await Action(action).ExecuteAsync(Parameters(), default)).Status, Is.EqualTo(ActionResultStatus.Succeeded));
        Assert.That(state.Snapshot.Channels["custom-mic"].Muted, Is.True);
    }
    [Test]
    public async Task Explicit_mute_and_unmute()
    {
        await Connect();
        var parameters = Parameters(); parameters["muted"] = "true";
        await Action("set-mute").ExecuteAsync(parameters, default);
        Assert.That(server.Muted, Is.True);
        parameters["muted"] = "false";
        await Action("set-mute").ExecuteAsync(parameters, default);
        Assert.That(server.Muted, Is.False);
    }
    [Test]
    public async Task Microphone_fallback_uses_discovered_id()
    {
        server.SupportedCommands = server.SupportedCommands.Where(x => x != "microphone.toggleMute").ToArray();
        await Connect();
        await Action("microphone-mute").ExecuteAsync(Parameters(), default);
        Assert.That(server.Commands, Does.Contain("channel.toggleMute"));
        Assert.That(server.Muted, Is.True);
    }
    [Test]
    public async Task Missing_channel_never_substitutes_another()
    {
        await Connect();
        var parameters = Parameters(); parameters["channelId"] = "deleted";
        Assert.That((await Action("toggle-mute").ExecuteAsync(parameters, default)).Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(server.Muted, Is.False);
    }
    [Test]
    public async Task Removal_invalidates_state_and_readdition_and_rename_update_cache()
    {
        await Connect();
        server.Event("channel.removed", new { channelId = "custom-mic" });
        await Until(() => state.Snapshot.Channels.Count == 0);
        server.ChannelName = "Renamed";
        server.Event("channel.added", server.ChannelState());
        await Until(() => state.Snapshot.Channels.TryGetValue("custom-mic", out var c) && c.Name == "Renamed");
    }
    [Test]
    public async Task Events_refresh_button_state()
    {
        await Connect();
        server.Muted = true; server.Event("channel.muteChanged", server.ChannelState());
        await Until(() => state.Snapshot.Channels["custom-mic"].Muted);
        var result = await Action("toggle-mute").GetActionStateAsync(Parameters(), default);
        Assert.That(result!.ActiveStateId, Is.EqualTo("muted"));
    }
    [Test]
    public async Task Concurrent_requests_are_correlated()
    {
        await Connect();
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => manager.ExecuteAsync("system.ping", new { }, default)));
        Assert.That(responses.All(x => x.GetProperty("pong").GetBoolean()), Is.True);
    }
    [Test]
    public async Task Timeout_aborts_session_and_does_not_replay_mutation()
    {
        await Connect();
        server.Hang = true;
        Assert.That((await Action("toggle-mute").ExecuteAsync(Parameters(), default)).Status, Is.EqualTo(ActionResultStatus.Failed));
        server.Hang = false;
        await Until(() => state.Snapshot.Connected && server.Authentications >= 2);
        Assert.That(server.Commands.Count(x => x == "channel.toggleMute"), Is.EqualTo(1));
    }
    [Test]
    public async Task Disconnect_reconnects_resubscribes_and_synchronizes()
    {
        await Connect();
        server.Volume = .83; server.Disconnect();
        await Until(() => server.Authentications >= 2 && state.Snapshot.Connected);
        Assert.That(state.Snapshot.Channels["custom-mic"].GameVolume, Is.EqualTo(.83));
        Assert.That(server.Subscriptions, Is.EqualTo(4));
    }
    [Test]
    public async Task Unavailable_capability_is_not_sent()
    {
        server.SupportedCommands = server.SupportedCommands.Where(x => x != "channel.setVolume").ToArray();
        await Connect();
        Assert.That((await Action("set-volume").ExecuteAsync(Parameters(), default)).Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(server.Commands, Does.Not.Contain("channel.setVolume"));
    }
    [Test]
    public async Task Shutdown_cancels_requests_and_reconnect_loop()
    {
        await Connect(); server.Hang = true;
        var pending = Action("toggle-mute").ExecuteAsync(Parameters(), default);
        await manager.DisposeAsync();
        Assert.That((await pending).Status, Is.EqualTo(ActionResultStatus.Failed));
        Assert.That(state.Snapshot.Connected, Is.False);
        var count = server.Authentications;
        await Task.Delay(300);
        Assert.That(server.Authentications, Is.EqualTo(count));
    }
    [Test]
    public async Task Host_harness_initializes_and_reads_disconnected_state()
    {
        await using var harness = PluginTestHarness.Create(builder =>
        {
            builder.Services.AddSingleton<MixaluneStateManager>();
            builder.Services.AddSingleton<MixaluneConnectionManager>();
            builder.RegisterIntegration<MixaluneIntegration>();
        });
        await harness.InitializeIntegrationsAsync();
        var result = await harness.Actions.ExecuteAsync("toggle-mute", Parameters());
        Assert.That(result.Succeeded, Is.False);
    }
    [Test]
    public async Task Server_restart_restores_connection()
    {
        await Connect();
        await server.DisposeAsync();
        await Until(() => !state.Snapshot.Connected);
        await server.StartAsync();
        await Until(() => state.Snapshot.Connected && server.Authentications == 2);
    }
    [Test]
    public async Task Oversized_event_disconnects_and_invalidates_cached_values()
    {
        await Connect();
        server.Event("channel.added", new { oversized = new string('x', 300000) });
        await Until(() => !state.Snapshot.Connected);
        var integration = new MixaluneIntegration(state, manager);
        Assert.That(await integration.ReadAsync("connected"), Is.EqualTo(MacroDeck.Sdk.Variables.VariableReading.Of(false)));
        Assert.That(await integration.ReadAsync("channel-" + MixaluneIntegration.ChannelKey("custom-mic") + "-volume"),
            Is.EqualTo(MacroDeck.Sdk.Variables.VariableReading.Unavailable));
    }
    [Test]
    public async Task State_snapshot_rename_preserves_channel_id()
    {
        await Connect();
        server.ChannelName = "Renamed by user";
        server.Event("system.stateChanged", new { channels = new[] { server.ChannelState() }, microphone = server.ChannelState() });
        await Until(() => state.Snapshot.Channels["custom-mic"].Name == "Renamed by user");
        Assert.That(state.Snapshot.Channels.Count, Is.EqualTo(1));
    }
    [Test]
    public void Newer_event_wins_over_confirmed_response()
    {
        state.OnResponse("channel.getState", JsonSerializer.SerializeToElement(server.ChannelState()));
        server.Volume = .9;
        state.OnEvent(JsonSerializer.SerializeToElement(new { @event = "channel.volumeChanged", data = server.ChannelState() }));
        Assert.That(state.Snapshot.Channels["custom-mic"].GameVolume, Is.EqualTo(.9));
    }
}



