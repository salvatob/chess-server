using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Game;
using ChessBotCore.Players;
using ChessBotCore.Search;

namespace App;

public class SocketPlayer : IPlayer {
    private readonly WebSocket _socket;
    private Timers _timers = new();

    /// <summary>
    /// The new <seealso cref="SocketPlayer"/> takes ownership of the web socket.
    /// </summary>
    public SocketPlayer(WebSocket socket, Timers timers) {
        _socket = socket;
        _timers = timers;
    }

    public SearchHandle ChooseMoveAsync(State state, Timers timers) {
        _timers = timers;
        // TODO no exception handling is really present here but it should
        var cts = new CancellationTokenSource();

        var task = Task.Run(async () => {
            await SendMessageAsync(new RequestMoveDto {
                Fen = state.GetFen(),
                WhiteTime = timers.WhiteTime,
                BlackTime = timers.BlackTime
            });
            
            MoveDtoMessage moveDtoMessage = await WaitMessageAsync<MoveDtoMessage>(cts.Token);
            
            MoveDTO moveDto = moveDtoMessage.Move;
            Move move = Move.FindFullMove(moveDto, state);
            
            return new SearchResults { BestMove = move };
        }, cts.Token);

        return new SearchHandle(cts, task);
    }

    public async Task OnGameStartAsync(bool yourColor, State state) {
        // Timers might not be set yet if ChooseMoveAsync hasn't been called.
        // But the game usually calls OnGameStartAsync first.
        // However, ChessGame.cs doesn't seem to pass timers to OnGameStartAsync.
        // I will keep it simple and send a message.
        await SendMessageAsync(new StartGameDto {
            Color = yourColor ? "white" : "black",
            InitialFen = state.GetFen(),
            WhiteTime = _timers.WhiteTime,
            BlackTime = _timers.BlackTime,
            Increment = _timers.Increment
        });
    }

    public async Task OnGameGameEndAsync(bool yourColor, GameResult result) {
        await SendMessageAsync(new EndGameDto {
            Result = result,
            Reason = null
        });
    }

    public Task OnErrorNotifyAsync(Exception error, bool gameEnd) {
        throw new NotImplementedException();
    }
    public Task OnErrorNotifyAsync(string errorMessage, bool gameEnd) {
        throw new NotImplementedException();
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
