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
                BlackTimeMs = timers.BlackTimeMs,
                LastMoveLAN = _moveHistory?.LastOrDefault().PrintLAN()
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

    private async Task CloseSocketAsync() {
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
        }
    }
    
    public async Task PrepareAsync(bool yourColor, State state, Timers timers, IReadOnlyList<Move> moveHistory) {
        _moveHistory = moveHistory;
        await SendMessageAsync(new PrepareGameDto {
            ColorWhite = yourColor,
            InitialFen = state.GetFen(),
            WhiteTimeMs = timers.WhiteTimeMs,
            BlackTimeMs = timers.BlackTimeMs
        });
    }

    public async Task OnGameStartAsync() {
        await SendMessageAsync(new GameStartedDto());
    }

    public async Task OnOpponentsMoveAsync(Move move, State newState) {
        await SendMessageAsync(new OpponentMoveDto {
            MoveLAN = move.PrintLAN(),
            FenAfter = newState.GetFen()
        });
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

    public async Task OnGameEndAsync(bool yourColor, GameResult result) {
        Console.WriteLine($"[DEBUG_LOG] OnGameEndAsync: sending EndGameDto. Current state: {_socket.State}");
        try {
            await SendMessageAsync(new EndGameDto {
                Result = result,
                Reason = result.GameEndReason.ToString()
            });
        } catch (Exception ex) {
            Console.WriteLine($"[DEBUG_LOG] OnGameEndAsync: Failed to send EndGameDto: {ex.Message}");
        }
        
        await CloseSocketAsync();
        Console.WriteLine("[DEBUG_LOG] OnGameEndAsync: Signaling _socketClosedTcs");
        _socketClosedTcs.TrySetResult();
    }

    public async Task OnErrorNotifyAsync(Exception error, bool gameEnd) {
        await SendMessageAsync(new ErrorMessageDto {
            Message = error.Message,
            GameEnd = gameEnd
        });
        if (gameEnd) _socketClosedTcs.TrySetResult();
    }

    public async Task OnErrorNotifyAsync(string errorMessage, bool gameEnd) {
        await SendMessageAsync(new ErrorMessageDto {
            Message = errorMessage,
            GameEnd = gameEnd
        });
        if (gameEnd) _socketClosedTcs.TrySetResult();
    }

    private async Task SendMessageAsync(OutgoingSocketMessage message) {
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

    private async Task<TMessage> WaitMessageAsync<TMessage>(CancellationToken ct) where TMessage : IncomingSocketMessage {
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
