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
    private readonly IWebSocketWrapper _socket;
    private IReadOnlyList<Move>? _moveHistory;
    
    /// <summary>
    /// The new <seealso cref="SocketPlayer"/> takes ownership of the web socket wrapper.
    /// </summary>
    public SocketPlayer(WebSocket socket) : this (new WebSocketWrapper(socket)) {
    }
    
    /// <summary>
    /// The new <seealso cref="SocketPlayer"/> takes ownership of the web socket wrapper.
    /// </summary>
    public SocketPlayer(IWebSocketWrapper socket) {
        _socket = socket;
    }

    /// <summary>
    /// Returns a task that completes when the underlying web socket is closed.
    /// </summary>
    /// <returns>A task representing the socket closure.</returns>
    public Task WaitForCloseAsync() => _socket.WaitForCloseAsync();

    /// <inheritdoc />
    public SearchHandle ChooseMoveAsync(State state, Timers timers) {
        // TODO no exception handling is really present here but it should
        var cts = new CancellationTokenSource();

        var task = Task.Run(async () => {
            await _socket.SendMessageAsync(new RequestMoveDto {
                Fen = state.GetFen(),
                WhiteTimeMs = timers.WhiteTimeMs,
                BlackTimeMs = timers.BlackTimeMs,
                LastMoveLAN = _moveHistory?.LastOrDefault().PrintLAN()
            });
            
            MoveDtoMessage moveDtoMessage = await _socket.WaitMessageAsync<MoveDtoMessage>(cts.Token);
            
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
    
    /// <inheritdoc />
    public async Task PrepareAsync(bool yourColor, State state, Timers timers, IReadOnlyList<Move> moveHistory) {
        _moveHistory = moveHistory;
        await _socket.SendMessageAsync(new PrepareGameDto {
            ColorWhite = yourColor,
            InitialFen = state.GetFen(),
            WhiteTimeMs = timers.WhiteTimeMs,
            BlackTimeMs = timers.BlackTimeMs
        });
    }

    /// <inheritdoc />
    public async Task OnGameStartAsync() {
        await _socket.SendMessageAsync(new GameStartedDto());
    }

    /// <inheritdoc />
    public async Task OnOpponentsMoveAsync(Move move, State newState) {
        await _socket.SendMessageAsync(new OpponentMoveDto {
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
        await _socket.SendMessageAsync(new StartGameDto {
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
            await _socket.SendMessageAsync(new EndGameDto {
                Result = result,
                Reason = result.GameEndReason.ToString()
            });
        } catch (Exception ex) {
            Console.WriteLine($"[DEBUG_LOG] OnGameEndAsync: Failed to send EndGameDto: {ex.Message}");
        }
        
        await _socket.CloseSocketAsync();
    }

    /// <inheritdoc />
    public async Task OnErrorNotifyAsync(Exception error, bool gameEnd) {
        await _socket.SendMessageAsync(new ErrorMessageDto {
            Message = error.Message,
            GameEnd = gameEnd
        });
    }

    /// <inheritdoc />
    public async Task OnErrorNotifyAsync(string errorMessage, bool gameEnd) {
        await _socket.SendMessageAsync(new ErrorMessageDto {
            Message = errorMessage,
            GameEnd = gameEnd
        });
    }

    /// <inheritdoc />
    public void Dispose() {
        _socket.Dispose();
    }
}
