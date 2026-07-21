using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Players;
using ChessBotCore.Search;

namespace App;

public class SocketPlayer : IPlayer {
    private readonly WebSocket _socket;

    public SocketPlayer(WebSocket socket) {
        _socket = socket;
    }

    public SearchHandle GetBestMove(State state, Timers timers) {
        // TODO no exception handling is really present here but it should
        var cts = new CancellationTokenSource();

        var task = Task.Run(async () => {
            await SendMessageAsync(new RequestMoveDto {
                State = state.ToString(),
                Timers = timers.ToString()
            });
            
            var moveDto = await WaitMessageAsync<MoveDto>(cts.Token);
            var move = Move.Parse(moveDto.Move);
            return new SearchResults { BestMove = move };
        }, cts.Token);

        return new SearchHandle(cts, task);
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
        await _socket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    private async Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : SocketMessage {
        byte[] buffer = new byte[1024 * 4];
        while (_socket.State == WebSocketState.Open) {
            ct.ThrowIfCancellationRequested();

            WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close) {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                throw new OperationCanceledException("Socket closed");
            }

            if (result.MessageType == WebSocketMessageType.Text) {
                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var message = JsonSerializer.Deserialize<SocketMessage>(json);
                if (message is TMessage specificMessage) {
                    return specificMessage;
                }
                // TODO If it's not the message we're waiting for, we might want to log it or ignore it.
                // For now, we continue waiting for the correct one.
            }
        }
        throw new InvalidOperationException("Socket closed while waiting for message.");
    }

    public void Dispose() {
        _socket.Dispose();
    }
}
