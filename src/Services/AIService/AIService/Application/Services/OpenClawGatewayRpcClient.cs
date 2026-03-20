using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AIService.Application.Services;

/// <summary>
///     OpenClaw Gateway WebSocket RPC 客户端（共享，供 Research 和 Automation 服务使用）
/// </summary>
internal sealed class OpenClawGatewayRpcClient : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly OpenClawOptions _options;
    private readonly ClientWebSocket _socket = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement?>> _pending = new();
    private readonly TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HashSet<string> _sentConnectKeys = new();
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _isConnected;

    public OpenClawGatewayRpcClient(OpenClawOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await _socket.ConnectAsync(new Uri(_options.GatewayUrl), cancellationToken);
        _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
        await SendConnectRequestAsync(cancellationToken);
        await _connected.Task.WaitAsync(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds), cancellationToken);
    }

    public async Task<JsonElement?> RequestAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + Random.Shared.Next(1000, 9999);
        var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;

        await SendJsonAsync(new
        {
            type = "req",
            id,
            method,
            @params = parameters
        }, cancellationToken);

        try
        {
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds), cancellationToken);
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                using var stream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await _socket.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        FailPending(new StateError("OpenClaw gateway disconnected"));
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(stream.ToArray());
                await HandleMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            FailPending(ex);
            if (!_connected.Task.IsCompleted)
                _connected.TrySetException(ex);
        }
    }

    private Task HandleMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Task.CompletedTask;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(message);
        }
        catch
        {
            return Task.CompletedTask;
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.TryGetProperty("event", out var eventNode) && eventNode.GetString() == "connect.challenge")
            {
                _logger.LogDebug("OpenClaw gateway sent connect.challenge; current gateway handshake does not require nonce replay");
                return Task.CompletedTask;
            }

            if (!root.TryGetProperty("id", out var idNode))
                return Task.CompletedTask;

            var id = idNode.GetString();
            if (string.IsNullOrWhiteSpace(id) || !_pending.TryRemove(id, out var completer))
                return Task.CompletedTask;

            if (root.TryGetProperty("ok", out var okNode) && okNode.ValueKind == JsonValueKind.True)
            {
                JsonElement? payload = null;
                if (root.TryGetProperty("payload", out var payloadNode))
                    payload = payloadNode.Clone();

                if (id.StartsWith("connect-", StringComparison.Ordinal) && !_connected.Task.IsCompleted)
                {
                    _isConnected = true;
                    _connected.TrySetResult();
                }

                completer.TrySetResult(payload);
                return Task.CompletedTask;
            }

            var errorMessage = root.TryGetProperty("error", out var errorNode) &&
                               errorNode.ValueKind == JsonValueKind.Object &&
                               errorNode.TryGetProperty("message", out var messageNode)
                ? messageNode.GetString()
                : root.TryGetProperty("message", out var fallbackMessageNode)
                    ? fallbackMessageNode.GetString()
                    : "OpenClaw request failed";

            var connectError = new InvalidOperationException(errorMessage);
            if (id.StartsWith("connect-", StringComparison.Ordinal) && !_connected.Task.IsCompleted)
                _connected.TrySetException(connectError);

            completer.TrySetException(connectError);
        }

        return Task.CompletedTask;
    }

    private async Task SendConnectRequestAsync(CancellationToken cancellationToken)
    {
        if (_isConnected)
            return;

        lock (_sentConnectKeys)
        {
            if (!_sentConnectKeys.Add("__default__"))
                return;
        }

        var connectId = $"connect-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(1000, 9999)}";
        var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[connectId] = tcs;

        await SendJsonAsync(new
        {
            type = "req",
            id = connectId,
            method = "connect",
            @params = new
            {
                minProtocol = 3,
                maxProtocol = 3,
                client = new
                {
                    id = _options.ClientId,
                    displayName = "Go Nomads AI Service",
                    version = "1.0.1",
                    platform = "dotnet",
                    mode = "backend",
                    instanceId = "travel-plan-openclaw-proxy"
                },
                caps = Array.Empty<string>(),
                role = "operator",
                scopes = new[]
                {
                    "operator.admin",
                    "operator.read",
                    "operator.write",
                    "operator.approvals",
                    "operator.pairing"
                },
                auth = new
                {
                    token = _options.GatewayToken
                }
            }
        }, cancellationToken);

        _ = tcs.Task.ContinueWith(task =>
        {
            if (task.IsFaulted)
                _logger.LogWarning(task.Exception, "OpenClaw connect attempt failed");
        }, TaskScheduler.Default);
    }

    private async Task SendJsonAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private void FailPending(Exception error)
    {
        foreach (var entry in _pending.ToArray())
        {
            if (_pending.TryRemove(entry.Key, out var pending))
                pending.TrySetException(error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _receiveCts?.Cancel();
            if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "dispose", CancellationToken.None);
        }
        catch
        {
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch
            {
            }
        }

        _socket.Dispose();
        _receiveCts?.Dispose();
    }

    internal sealed class StateError : Exception
    {
        public StateError(string message) : base(message) { }
    }
}
