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

/// <summary>
/// Implementation of an <seealso cref="IPlayer"/>, that is mean to represent a player that is connected via a web socket.
/// </summary>
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

    /// <summary>
    /// Returns a task that completes when the underlying web socket is closed.
    /// </summary>
    /// <returns>A task representing the socket closure.</returns>
    public Task WaitForCloseAsync() => _socketClosedTcs.Task;

    /// <inheritdoc />
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

    /// <summary>
    /// Initiates a graceful close of the web socket connection.
    /// </summary>
    /// <returns>A task representing the closing operation.</returns>
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
    
    /// <inheritdoc />
    public async Task PrepareAsync(bool yourColor, State state, Timers timers, IReadOnlyList<Move> moveHistory) {
        _moveHistory = moveHistory;
        await SendMessageAsync(new PrepareGameDto {
            ColorWhite = yourColor,
            InitialFen = state.GetFen(),
            WhiteTimeMs = timers.WhiteTimeMs,
            BlackTimeMs = timers.BlackTimeMs
        });
    }

    /// <inheritdoc />
    public async Task OnGameStartAsync() {
        await SendMessageAsync(new GameStartedDto());
    }

    /// <inheritdoc />
    public async Task OnOpponentsMoveAsync(Move move, State newState) {
        await SendMessageAsync(new OpponentMoveDto {
            MoveLAN = move.PrintLAN(),
            FenAfter = newState.GetFen()
        });
    }

    /// <summary>
    /// Notifies the player that the game has officially started with initial state information.
    /// </summary>
    /// <param name="yourColor">The color assigned to this player. True for white, false for black.</param>
    /// <param name="state">The initial state of the game board.</param>
    /// <param name="timers">The time settings for both players.</param>
    /// <param name="moveHistory">A reference to the list of moves played so far.</param>
    /// <returns>A task representing the notification process.</returns>
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task OnErrorNotifyAsync(Exception error, bool gameEnd) {
        await SendMessageAsync(new ErrorMessageDto {
            Message = error.Message,
            GameEnd = gameEnd
        });
        if (gameEnd) _socketClosedTcs.TrySetResult();
    }

    /// <inheritdoc />
    public async Task OnErrorNotifyAsync(string errorMessage, bool gameEnd) {
        await SendMessageAsync(new ErrorMessageDto {
            Message = errorMessage,
            GameEnd = gameEnd
        });
        if (gameEnd) _socketClosedTcs.TrySetResult();
    }

    /// <summary>
    /// Serializes and sends a message through the web socket.
    /// </summary>
    /// <param name="message">The outgoing message to send.</param>
    /// <returns>A task representing the sending operation.</returns>
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

    /// <summary>
    /// Waits for a specific type of message from the web socket.
    /// </summary>
    /// <typeparam name="TMessage">The type of the expected incoming message.</typeparam>
    /// <param name="ct">A cancellation token to observe while waiting for the message.</param>
    /// <returns>The received message of the specified type.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the socket is closed or the token is cancelled.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the socket state is invalid.</exception>
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
