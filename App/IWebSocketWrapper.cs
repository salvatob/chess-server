using System.Net.WebSockets;
using App.Dtos;

namespace App;

/// <summary>
/// Interface for a wrapper around a web socket to encapsulate message sending and receiving.
/// </summary>
public interface IWebSocketWrapper : IDisposable {
    /// <summary>
    /// Gets the current state of the underlying web socket.
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// Returns a task that completes when the underlying web socket is closed.
    /// </summary>
    Task WaitForCloseAsync();

    /// <summary>
    /// Serializes and sends a message through the web socket.
    /// </summary>
    Task SendMessageAsync(OutgoingSocketMessage message);

    /// <summary>
    /// Waits for a specific type of message from the web socket.
    /// </summary>
    Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage;

    /// <summary>
    /// Initiates a graceful close of the web socket connection.
    /// </summary>
    Task CloseSocketAsync();
}
