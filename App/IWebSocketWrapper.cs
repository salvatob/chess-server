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
    /// <param name="message">The outgoing message to send.</param>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task SendMessageAsync(OutgoingSocketMessage message);

    /// <summary>
    /// Waits for a specific type of message from the web socket.
    /// </summary>
    /// <typeparam name="TMessage">The type of the expected incoming message.</typeparam>
    /// <param name="ct">A cancellation token to observe while waiting for the message.</param>
    /// <returns>A task that represents the asynchronous wait operation. The task result contains the received message.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled or the socket is closed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the socket state becomes invalid.</exception>
    Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage;

    /// <summary>
    /// Initiates a graceful close of the web socket connection.
    /// </summary>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task CloseSocketAsync();
}
