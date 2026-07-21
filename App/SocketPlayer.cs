using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Players;

namespace App;

public class SocketPlayer : IPlayer {
    private readonly WebSocket _socket;
    private TaskCompletionSource<string>? _moveCompletionSource;

    public SocketPlayer(WebSocket socket) {
        _socket = socket;
    }

    public SearchHandle GetBestMove(State state, Timers timers) {
        // Send RequestMove to client
        SendMessageAsync(new RequestMoveDto()).GetAwaiter().GetResult();

        _moveCompletionSource = new TaskCompletionSource<string>();
        
        // Wait for MoveDto to be received via the receive loop
        string moveStr = _moveCompletionSource.Task.GetAwaiter().GetResult();
        
        Move move = Move.Parse(moveStr, state);
        var results = new ChessBotCore.Search.SearchResults { BestMove = move };
        var task = Task.FromResult(results);
        return new SearchHandle(new CancellationTokenSource(), task);
    }

    public async Task StartGameAsync(string color, string fen) {
        await SendMessageAsync(new StartGameDto {
            Color = color,
            InitialFen = fen
        });
    }

    public async Task EndGameAsync(string result, string? reason = null) {
        await SendMessageAsync(new EndGameDto {
            Result = result,
            Reason = reason
        });
    }

    private async Task SendMessageAsync(SocketMessage message) {
        if (_socket.State != WebSocketState.Open) return;
        
        byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(message);
        await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task RunReceiveLoopAsync(CancellationToken ct) {
        byte[] buffer = new byte[1024 * 4];
        while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested) {
            WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close) {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            } else if (result.MessageType == WebSocketMessageType.Text) {
                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var message = JsonSerializer.Deserialize<SocketMessage>(json);
                if (message is MoveDto moveDto) {
                    _moveCompletionSource?.TrySetResult(moveDto.Move);
                }
            }
        }
    }

    public void Dispose() {
        _socket.Dispose();
    }
}
