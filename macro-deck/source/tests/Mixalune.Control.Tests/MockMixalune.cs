using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mixalune.Control.Tests;

#pragma warning disable CA1051 // Mutable fault-injection fixture fields; never shipped.
/// <summary>Contract fixture, not evidence that a shipping Mixalune or Macro Deck build works.</summary>
public sealed class MockMixalune : IAsyncDisposable
{
    public const string Token = "test-only-token-with-at-least-32-characters";
    WebApplication? app;
    readonly ConcurrentDictionary<WebSocket, Channel<object>> clients = new();
    public int Port { get; private set; }
    public int Authentications;
    public int Subscriptions;
    public bool Hang;
    public string ApiVersion = "1.0";
    public double Volume = .5;
    public double MonitorVolume = .4;
    public bool Muted;
    public string ChannelName = "Custom microphone name";
    public readonly ConcurrentQueue<string> Commands = new();
    public string[] SupportedCommands = ["system.getVersion", "system.getCapabilities", "system.getState", "system.ping", "channel.list",
        "channel.getState", "channel.setVolume", "channel.adjustVolume", "channel.setMute", "channel.toggleMute", "microphone.toggleMute"];
    public object ChannelState() => new { channelId = "custom-mic", name = ChannelName, gameVolume = Volume, monitorVolume = MonitorVolume, volume = Volume, muted = Muted };
    public async Task StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, Port));
        app = builder.Build();
        app.UseWebSockets();
        app.Run(HandleAsync);
        await app.StartAsync();
        Port = new Uri(app.Urls.Single()).Port;
    }
    async Task HandleAsync(HttpContext context)
    {
        if (context.Request.Path != "/api/v1" || context.Request.QueryString.HasValue || context.Request.Headers.ContainsKey("Origin")) { context.Response.StatusCode = 403; return; }
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var outgoing = Channel.CreateUnbounded<object>();
        clients[socket] = outgoing;
        var writer = Task.Run(async () =>
        {
            try
            {
                await foreach (var value in outgoing.Reader.ReadAllAsync(context.RequestAborted))
                    await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(value).AsMemory(), WebSocketMessageType.Text, true, context.RequestAborted);
            }
            catch (Exception) { }
        });
        var authenticated = false;
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var buffer = new byte[16384];
                var result = await socket.ReceiveAsync(buffer.AsMemory(), context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close) break;
                using var doc = JsonDocument.Parse(buffer.AsMemory(0, result.Count));
                var root = doc.RootElement;
                var id = root.GetProperty("id").GetString();
                var action = root.GetProperty("action").GetString()!;
                var args = root.GetProperty("params");
                Commands.Enqueue(action);
                if (Hang) continue;
                if (!authenticated)
                {
                    if (action != "auth.authenticate" || args.GetProperty("token").GetString() != Token)
                    {
                        outgoing.Writer.TryWrite(new { apiVersion = ApiVersion, id, type = "response", success = false, error = new { code = "UNAUTHORIZED", message = Token } });
                        continue;
                    }
                    authenticated = true;
                    Interlocked.Increment(ref Authentications);
                }
                object data;
                switch (action)
                {
                    case "auth.authenticate": data = new { authenticated = true }; break;
                    case "system.getCapabilities": data = new { commands = SupportedCommands, edition = (string?)null, eventCategories = new[] { "channel", "microphone", "system" } }; break;
                    case "system.getVersion": data = new { version = "test", apiVersion = ApiVersion }; break;
                    case "channel.list": data = new { channels = new[] { ChannelState() } }; break;
                    case "system.getState": data = new { channels = new[] { ChannelState() }, microphone = ChannelState() }; break;
                    case "events.subscribe": Interlocked.Increment(ref Subscriptions); data = new { }; break;
                    case "system.ping": data = new { pong = true }; break;
                    default:
                        if (args.TryGetProperty("channelId", out var channelId) && channelId.GetString() != "custom-mic")
                        {
                            outgoing.Writer.TryWrite(new { apiVersion = ApiVersion, id, type = "response", success = false, error = new { code = "CHANNEL_NOT_FOUND", message = "Missing" } });
                            continue;
                        }
                        bool monitor = args.TryGetProperty("bus", out var bus) && bus.GetString() == "monitor";
                        if (action == "channel.setVolume")
                        {
                            if (monitor) MonitorVolume = args.GetProperty("volume").GetDouble();
                            else Volume = args.GetProperty("volume").GetDouble();
                        }
                        if (action == "channel.adjustVolume")
                        {
                            if (monitor) MonitorVolume = Math.Clamp(MonitorVolume + args.GetProperty("delta").GetDouble(), 0, 1);
                            else Volume = Math.Clamp(Volume + args.GetProperty("delta").GetDouble(), 0, 1);
                        }
                        if (action.EndsWith("toggleMute", StringComparison.Ordinal)) Muted = !Muted;
                        if (action == "channel.setMute") Muted = args.GetProperty("muted").GetBoolean();
                        data = ChannelState();
                        break;
                }
                outgoing.Writer.TryWrite(new { apiVersion = ApiVersion, id, type = "response", success = true, data });
            }
        }
        catch (Exception) { }
        finally { clients.TryRemove(socket, out _); outgoing.Writer.TryComplete(); await writer; }
    }
    public void Event(string name, object data)
    {
        foreach (var outgoing in clients.Values)
            outgoing.Writer.TryWrite(new { apiVersion = "1.0", type = "event", @event = name, data });
    }
    public void Disconnect() { foreach (var socket in clients.Keys) socket.Abort(); }
    public async ValueTask DisposeAsync()
    {
        Disconnect();
        if (app is not null) { await app.StopAsync(); await app.DisposeAsync(); app = null; }
    }
}
