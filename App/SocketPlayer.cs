using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Board;
using ChessBotCore.Game;
using ChessBotCore.Players;
using ChessBotCore.Search;

namespace App;

public class SocketPlayer : IPlayer {
    private readonly WebSocket _socket;
    private readonly TaskCompletionSource _socketClosedTcs = new();
    private IReadOnlyList<Move>? _moveHistory;
    /// <summary>
    /// The new <seealso cref="SocketPlayer"/> takes ownership of the web socket.
    /// </summary>
    public SocketPlayer(WebSocket socket) {
        _socket = socket;
    }

    public Task WaitForCloseAsync() => _socketClosedTcs.Task;

    public SearchHandle ChooseMoveAsync(State state, Timers timers) {
        // TODO no exception handling is really present here but it should
        var cts = new CancellationTokenSource();

        var task = Task.Run(async () => {
            await SendMessageAsync(new RequestMoveDto {
                Fen = state.GetFen(),
                WhiteTimeMs = timers.WhiteTimeMs,
                BlackTimeMs = timers.BlackTimeMs
            });
            
            MoveDtoMessage moveDtoMessage = await WaitMessageAsync<MoveDtoMessage>(cts.Token);
            
            if (moveDtoMessage.Move == null)
                throw new InvalidOperationException("Move data is missing from message.");

            // Convert string coordinates (e.g., "e2") to 1D integers
            int from = Coordinates.FromString(moveDtoMessage.Move.From).To1D();
            int to = Coordinates.FromString(moveDtoMessage.Move.To).To1D();
            
            // Create the engine's expected MoveDTO
            MoveDTO engineMoveDto = new MoveDTO(from, to, moveDtoMessage.Move.Promotion);
            
            Move move = Move.FindFullMove(engineMoveDto, state);
            
            return new SearchResults { BestMove = move };
        }, cts.Token);

        return new SearchHandle(cts, task);
    }

    public async Task OnGameStartAsync(bool yourColor, State state, Timers timers, IReadOnlyList<Move> moveHistory) {
        _moveHistory = moveHistory;
        await SendMessageAsync(new StartGameDto {
            ColorWhite = yourColor ,
            InitialFen = state.GetFen(),
            WhiteTimeMs = timers.WhiteTimeMs,
            BlackTimeMs = timers.BlackTimeMs,
            IncrementMs = timers.IncrementMs
        });
    }

    public async Task OnGameGameEndAsync(bool yourColor, GameResult result) {
        await SendMessageAsync(new EndGameDto {
            Result = result,
            Reason = result.GameEndReason.ToString()
        });
        _socketClosedTcs.TrySetResult();
    }

    public Task OnErrorNotifyAsync(Exception error, bool gameEnd) {
        if (gameEnd) _socketClosedTcs.TrySetResult();
        return Task.CompletedTask;
    }
    public Task OnErrorNotifyAsync(string errorMessage, bool gameEnd) {
        if (gameEnd) _socketClosedTcs.TrySetResult();
        return Task.CompletedTask;
    }

    private async Task SendMessageAsync(OutgoingSocketMessage message) {
        if (_socket.State != WebSocketState.Open) return;

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

    private async Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage {
        byte[] buffer = new byte[1024 * 4];
        while (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseSent) {
            ct.ThrowIfCancellationRequested();

            WebSocketReceiveResult result;
            try {
                result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            } catch (WebSocketException) {
                _socketClosedTcs.TrySetResult();
                break;
            }
            
            if (result.MessageType == WebSocketMessageType.Close) {
                if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived) {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                }
                _socketClosedTcs.TrySetResult();
                throw new OperationCanceledException("Socket closed");
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
        // TODO maybe this should be called on Dispose instead
        _socketClosedTcs.TrySetResult();
        throw new InvalidOperationException("Socket closed while waiting for message.");
    }


    public void Dispose() {
        _socket.Dispose();
    }
}
