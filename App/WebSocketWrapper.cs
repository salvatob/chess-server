using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Dtos;

namespace App;

/// <summary>
/// Implementation of <see cref="IWebSocketWrapper"/> that wraps a <see cref="WebSocket"/>.
/// </summary>
public class WebSocketWrapper : IWebSocketWrapper {
    private readonly WebSocket _socket;
    private readonly TaskCompletionSource _socketClosedTcs = new();

    public WebSocketWrapper(WebSocket socket) {
        _socket = socket;
    }

    /// <inheritdoc />
    public WebSocketState State => _socket.State;

    /// <inheritdoc />
    public Task WaitForCloseAsync() => _socketClosedTcs.Task;

    /// <inheritdoc />
    public async Task SendMessageAsync(OutgoingSocketMessage message) {
        if (_socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived) return;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(message, options);
        Console.WriteLine($"[DEBUG_LOG] Sending JSON: {Encoding.UTF8.GetString(buffer)}");
        await _socket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    /// <inheritdoc />
    public async Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage {
        byte[] buffer = new byte[1024 * 4];
        try {
            while (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseSent) {
                ct.ThrowIfCancellationRequested();

                WebSocketReceiveResult result;
                try {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                } catch (OperationCanceledException) {
                    Console.WriteLine("[DEBUG_LOG] WaitMessageAsync: ReceiveAsync cancelled");
                    throw;
                } catch (WebSocketException ex) {
                    Console.WriteLine($"[DEBUG_LOG] WaitMessageAsync: WebSocketException: {ex.Message}");
                    break;
                }
                
                if (result.MessageType == WebSocketMessageType.Close) {
                    Console.WriteLine($"[DEBUG_LOG] WaitMessageAsync: Received Close message. State: {_socket.State}");
                    if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived) {
                        using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", closeCts.Token);
                    }
                    throw new OperationCanceledException("Socket closed by remote peer");
                }

                if (result.MessageType == WebSocketMessageType.Text) {
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[DEBUG_LOG] Received JSON: {json}");
                    var options = new JsonSerializerOptions {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    var message = JsonSerializer.Deserialize<IncomingSocketMessage>(json, options);
                    if (message is TMessage specificMessage) {
                        return specificMessage;
                    }
                }
            }
        } finally {
            // We only signal closure if the socket is actually closed or aborted
            if (_socket.State is WebSocketState.Closed or WebSocketState.Aborted) {
                Console.WriteLine($"[DEBUG_LOG] WaitMessageAsync: Socket is {_socket.State}. Signaling _socketClosedTcs");
                _socketClosedTcs.TrySetResult();
            }
        }
        throw new InvalidOperationException($"Socket state is {_socket.State} while waiting for message.");
    }

    /// <inheritdoc />
    public async Task CloseSocketAsync() {
        Console.WriteLine($"[DEBUG_LOG] CloseSocketAsync: Current state: {_socket.State}");
        try {
            // Using a timeout for the close handshake to ensure we don't hang forever 
            // if the client is unresponsive, but still giving it a chance to finish.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Console.WriteLine("[DEBUG_LOG] CloseSocketAsync: Calling CloseAsync");
            await _socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Game finished",
                cts.Token);
            Console.WriteLine($"[DEBUG_LOG] CloseSocketAsync: CloseAsync finished. State: {_socket.State}");
        } catch (Exception ex) {
            Console.WriteLine($"[DEBUG_LOG] Error during socket close: {ex.Message}");
        } finally {
             _socketClosedTcs.TrySetResult();
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Console.WriteLine($"[DEBUG_LOG] Dispose: Current state: {_socket.State}");
        // Only signal if not already signaled by graceful close
        _socketClosedTcs.TrySetResult();
        
        // We should always dispose to release resources, 
        // but if we are in the middle of a graceful close (CloseSent), 
        // calling Dispose() might abort the connection before the client receives the close frame.
        // However, Dispose() is meant to be the final cleanup.
        // The real synchronization happens via WaitForCloseAsync().
        _socket.Dispose();
        Console.WriteLine("[DEBUG_LOG] Dispose: Socket disposed");
    }
}
