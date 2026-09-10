using System.Net.WebSockets;
using App.Dtos;

namespace App.UnitTests;

public class MockWebSocketWrapper : IWebSocketWrapper {
    public WebSocketState State { get; set; } = WebSocketState.Open;
    public List<OutgoingSocketMessage> SentMessages { get; } = new();
    public Queue<IncomingSocketMessage> IncomingMessages { get; } = new();
    public List<string> CallOrder { get; } = new();
    public bool Disposed { get; private set; }
    
    private readonly Queue<TaskCompletionSource<IncomingSocketMessage>> _waiters = new();

    public Task WaitForCloseAsync() => Task.CompletedTask;

    public Task SendMessageAsync(OutgoingSocketMessage message) {
        CallOrder.Add(nameof(SendMessageAsync));
        SentMessages.Add(message);
        return Task.CompletedTask;
    }

    public async Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage {
        CallOrder.Add(nameof(WaitMessageAsync));
        
        if (IncomingMessages.Count > 0) {
            return (TMessage)IncomingMessages.Dequeue();
        }

        var tcs = new TaskCompletionSource<IncomingSocketMessage>();
        _waiters.Enqueue(tcs);
        
        using (ct.Register(() => tcs.TrySetCanceled(ct))) {
            var result = await tcs.Task;
            return (TMessage)result;
        }
    }

    public void PushMessage(IncomingSocketMessage message) {
        if (_waiters.TryDequeue(out var tcs)) {
            tcs.TrySetResult(message);
        } else {
            IncomingMessages.Enqueue(message);
        }
    }

    public void TriggerWaitException(Exception ex) {
        if (_waiters.TryDequeue(out var tcs)) {
            tcs.TrySetException(ex);
        }
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
