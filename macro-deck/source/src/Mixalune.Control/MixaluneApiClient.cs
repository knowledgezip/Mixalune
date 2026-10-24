using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace Mixalune.Control;

public sealed class MixaluneApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>One socket/session. Requests are never retried or transferred to another session.</summary>
public sealed class MixaluneApiClient : IAsyncDisposable
{
    const int MaximumMessageBytes = 256 * 1024;
    readonly ClientWebSocket socket = new();
    readonly SemaphoreSlim sendGate = new(1);
    readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> pending = new();
    readonly ConcurrentDictionary<string, string> commands = new();
    readonly CancellationTokenSource lifetime = new();
    readonly TimeSpan requestTimeout;
    Task? reader;
    public event Action<JsonElement>? EventReceived;
    public event Action<string, JsonElement>? ResponseReceived;
    public Task Completion => reader ?? Task.CompletedTask;

    public MixaluneApiClient(TimeSpan requestTimeout)
    {
        this.requestTimeout = requestTimeout;
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        socket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(10);
        socket.Options.Proxy = null;
    }

    public async Task ConnectAsync(int port, string token, TimeSpan timeout, CancellationToken cancellation)
    {
        if (port is < 1024 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (token.Length is < 32 or > 128) throw new MixaluneApiException("TOKEN", "Enter the token copied from Mixalune.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token);
        deadline.CancelAfter(timeout);
        await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/api/v1"), deadline.Token).ConfigureAwait(false);
        reader = ReceiveAsync();
        var result = await CommandAsync("auth.authenticate", new { token }, deadline.Token).ConfigureAwait(false);
        if (!result.TryGetProperty("authenticated", out var authenticated) || authenticated.ValueKind != JsonValueKind.True)
            throw new MixaluneApiException("UNAUTHORIZED", "Mixalune rejected authentication.");
    }

    public async Task<JsonElement> CommandAsync(string action, object parameters, CancellationToken cancellation = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token);
        deadline.CancelAfter(requestTimeout);
        var id = Guid.NewGuid().ToString("N");
        var response = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = response;
        commands[id] = action;
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { apiVersion = "1.0", id, type = "command", action, @params = parameters });
            if (bytes.Length > 16384) throw new MixaluneApiException("SIZE", "Command exceeds Mixalune's message limit.");
            await sendGate.WaitAsync(deadline.Token).ConfigureAwait(false);
            try { await socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, deadline.Token).ConfigureAwait(false); }
            finally { sendGate.Release(); }
            return await response.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested && !lifetime.IsCancellationRequested)
        {
            // The server may have applied a timed-out mutation. Disconnect and synchronize; never replay it.
            Abort();
            throw new MixaluneApiException("TIMEOUT", "Mixalune did not respond in time. The command was not retried.");
        }
        catch (OperationCanceledException) { Abort(); throw; }
        finally { pending.TryRemove(id, out _); commands.TryRemove(id, out _); }
    }

    async Task ReceiveAsync()
    {
        try
        {
            var buffer = new byte[MaximumMessageBytes];
            while (!lifetime.IsCancellationRequested)
            {
                var used = 0;
                ValueWebSocketReceiveResult part;
                do
                {
                    part = await socket.ReceiveAsync(buffer.AsMemory(used), lifetime.Token).ConfigureAwait(false);
                    if (part.MessageType == WebSocketMessageType.Close) return;
                    if (part.MessageType != WebSocketMessageType.Text) throw ProtocolError();
                    used += part.Count;
                    if (!part.EndOfMessage && used == buffer.Length) throw ProtocolError();
                } while (!part.EndOfMessage);
                using var document = JsonDocument.Parse(buffer.AsMemory(0, used), new() { MaxDepth = 24 });
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || root.GetProperty("apiVersion").GetString() != "1.0")
                    throw new MixaluneApiException("VERSION", "This plugin requires Mixalune local API 1.0.");
                var type = root.GetProperty("type").GetString();
                if (type == "event")
                {
                    if (root.GetProperty("event").ValueKind != JsonValueKind.String || root.GetProperty("data").ValueKind != JsonValueKind.Object) throw ProtocolError();
                    EventReceived?.Invoke(root.Clone());
                }
                else if (type == "response")
                {
                    var id = root.GetProperty("id");
                    if (id.ValueKind == JsonValueKind.Null) throw ProtocolError();
                    if (id.ValueKind != JsonValueKind.String) throw ProtocolError();
                    if (!pending.TryGetValue(id.GetString()!, out var request)) continue;
                    var success = root.GetProperty("success");
                    if (success.ValueKind == JsonValueKind.True)
                    {
                        var data = root.GetProperty("data").Clone();
                        if (commands.TryGetValue(id.GetString()!, out var command)) ResponseReceived?.Invoke(command, data);
                        request.TrySetResult(data);
                    }
                    else if (success.ValueKind == JsonValueKind.False)
                    {
                        var code = root.GetProperty("error").GetProperty("code").GetString() ?? "API_ERROR";
                        // Do not display arbitrary server text, which could contain credentials.
                        request.TrySetException(new MixaluneApiException(code, FriendlyError(code)));
                    }
                    else throw ProtocolError();
                }
                else throw ProtocolError();
            }
        }
        catch (Exception ex)
        {
            var error = ex as MixaluneApiException ?? ProtocolError();
            foreach (var request in pending.Values) request.TrySetException(error);
            throw;
        }
        finally
        {
            lifetime.Cancel();
            foreach (var request in pending.Values) request.TrySetException(new MixaluneApiException("DISCONNECTED", "Mixalune disconnected. No commands were replayed."));
            socket.Abort();
        }
    }

    public static string FriendlyError(string code) => code switch
    {
        "UNAUTHORIZED" => "Authentication failed. Copy the current token from Mixalune.",
        "CHANNEL_NOT_FOUND" => "The saved channel is unavailable. Choose a replacement in this action.",
        "FEATURE_NOT_LICENSED" => "This function is unavailable under Mixalune's current application policy.",
        "UNSUPPORTED_FEATURE" or "UNKNOWN_COMMAND" => "This version of Mixalune does not support that function.",
        "INVALID_PARAMETERS" => "Mixalune rejected the action parameters.",
        _ => "Mixalune could not complete the request."
    };
    static MixaluneApiException ProtocolError() => new("PROTOCOL", "Mixalune returned an invalid or oversized API message.");
    public void Abort() { lifetime.Cancel(); socket.Abort(); }
    public async ValueTask DisposeAsync()
    {
        Abort();
        if (reader is not null) { try { await reader.ConfigureAwait(false); } catch { } }
        socket.Dispose();
        // The cancellation source/semaphore may still be observed by completing callers.
    }
}
