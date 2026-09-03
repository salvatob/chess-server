using System.Net.WebSockets;
using App.Dtos;

namespace App.UnitTests;

public class MockWebSocketWrapper : IWebSocketWrapper {
    public WebSocketState State { get; set; } = WebSocketState.Open;
    public List<OutgoingSocketMessage> SentMessages { get; } = new();
    public Queue<IncomingSocketMessage> IncomingMessages { get; } = new();
    public List<string> CallOrder { get; } = new();
    public bool Disposed { get; private set; }
    
    public Task WaitForCloseAsync() => Task.CompletedTask;

    public Task SendMessageAsync(OutgoingSocketMessage message) {
        CallOrder.Add(nameof(SendMessageAsync));
        SentMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage {
        CallOrder.Add(nameof(WaitMessageAsync));
        if (IncomingMessages.Count == 0) {
            throw new InvalidOperationException("No incoming messages queued in mock.");
        }
        return Task.FromResult((TMessage)IncomingMessages.Dequeue());
    }

    public Task CloseSocketAsync() {
        CallOrder.Add(nameof(CloseSocketAsync));
        State = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public void Dispose() {
        CallOrder.Add(nameof(Dispose));
        Disposed = true;
    }
}
