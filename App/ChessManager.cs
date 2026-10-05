using System.Collections.Concurrent;
using System.Threading.Channels;
using ChessBotCore;
using ChessBotCore.Game;

namespace App;

/// <summary>
/// Class responsible for managing all chess games. Naturally is fully thread safe.
/// </summary>
public class ChessManager {
    private readonly TimeSpan _builderTimeout = TimeSpan.FromHours(1);
    
    // // possibly needed for things like player reconnecting, or spectating, 
    // private readonly ConcurrentDictionary<int, ChessGame> _runningGames = new();
    private readonly ConcurrentDictionary<int, GameBuilder> _gameBuilders = new();
    private readonly Channel<ChessGame> _gameQueue = Channel.CreateUnbounded<ChessGame>();
    
    private int _idSeed = 1;


    /// <summary>
    /// Initializes a new instance of the <see cref="ChessManager"/> class.
    /// </summary>
    /// <param name="maxConcurrentGames">The maximum number of games that can run simultaneously.</param>
    public ChessManager(int maxConcurrentGames = 1) {
        for (int i = 0; i < maxConcurrentGames; i++) {
            _ = Task.Run(GameWorkerAsync);
        }
    }


    /// <summary>
    /// Creates a new game with default settings (5 minutes for both playres, 2 seconds increment)
    /// </summary>
    /// <returns> The id number of the game created that can be used to further build the game.</returns>
    public int CreateGame() {
        return CreateGame(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));
    }


    /// <summary>
    /// Registers a game builder object in internal storage. Returns the id to that builder.
    /// </summary>
    /// <param name="whiteTime">The base time for the white player.</param>
    /// <param name="blackTime">The base time for the black player.</param>
    /// <param name="increment">The time increment per move.</param>
    /// <param name="fen">Optional initial board state in FEN format.</param>
    /// <returns> The id number of the game created that can be used to further build the game.</returns>
    public int CreateGame(TimeSpan whiteTime, TimeSpan blackTime, TimeSpan increment, string? fen = null) {
        int id = Interlocked.Increment(ref _idSeed);
        // We don't create the ChessGame yet because we need players.
        // Or we could store a placeholder.
        var builder = new GameBuilder {
            Timers = new Timers {
                BaseWhiteTime = whiteTime,
                BaseBlackTime = blackTime,
                Increment = increment,
                WhiteTime = whiteTime,
                BlackTime = blackTime
            }
        };

        if (!string.IsNullOrWhiteSpace(fen)) {
            builder.State = State.FromFen(fen);
        }

        _gameBuilders[id] = builder;
        
        // to prevent 'zombie' builders that never get started
        _ = RemoveBuilderAfterTimeout(id);
        return id;
    }
    

    
    /// <summary>
    /// Registers an initialized player to a game builder.
    /// </summary>
    /// <param name="builderId">Id of the builder we are trying to </param>
    /// <param name="player">The player to register.</param>
    /// <param name="white">The color/side of the chess game we are trying to register the player to.</param>
    /// <exception cref="InvalidOperationException">When the player is already registered or the game is not ready.</exception>
    /// <exception cref="KeyNotFoundException">When the builder is not found.</exception>
    public void RegisterPlayer(int builderId, IPlayer player, bool white) {
        var gameBuilder = _gameBuilders[builderId];

        lock (gameBuilder) {
            if (white) {
                if (gameBuilder.WhiteIsSet) {
                    throw new InvalidOperationException("White player already set.");
                }

                gameBuilder.WhitePlayer = player;
            } else {
                if (gameBuilder.BlackIsSet) {
                    throw new InvalidOperationException("Black player already set.");
                }

                gameBuilder.BlackPlayer = player;
            }
        }
    }

    /// <summary>
    /// Constructs a <see cref="ChessGame"/> from a registered builder and adds it to the game queue.
    /// </summary>
    /// <param name="builderId">The ID of the game builder to use.</param>
    /// <exception cref="InvalidOperationException">When the game is not ready.</exception>
    /// <exception cref="KeyNotFoundException">When the builder is not found.</exception>
    public void StartGame(int builderId) {
        if (!_gameBuilders.TryGetValue(builderId, out var builder)) {
            throw new KeyNotFoundException($"A builder with id {builderId} was not found.");
        }

        ChessGame? game = null;
        bool gameReady = false;

        lock (builder) {
            if (builder.TryBuild(out game)) {
                gameReady = true;
                _gameBuilders.TryRemove(builderId, out _);
            }
        }

        if (!gameReady) {
            throw new InvalidOperationException("Players are not set yet.");
        }

        builder.Dispose();
        // forcing builder.Dispose() is mostly for peace of mind, since ownership of all data
        // has been transferred out of the builder

        if (!_gameQueue.Writer.TryWrite(game!)) {
            game!.Dispose();
            throw new InvalidOperationException("Failed to enqueue game");
        }
    }

    /// <summary>
    /// Removes and disposes a game builder from the internal storage after a timeout.
    /// </summary>
    /// <param name="id">If id is not found, it does nothing</param>
    private async Task RemoveBuilderAfterTimeout(int id) {
        await Task.Delay(_builderTimeout);

        if (_gameBuilders.TryRemove(id, out var builder)) {
            builder.Dispose();
        }
    }
    
    /// <summary>
    /// Processes and plays games from the internal game queue.
    /// </summary>
    /// <returns>A task representing the worker process.</returns>
    private async Task GameWorkerAsync() {
        await foreach (var game in _gameQueue.Reader.ReadAllAsync()) {
            try {
                await game.PlayAsync();
            }
            catch (OperationCanceledException) {
                Console.WriteLine("[DEBUG_LOG] Game enqueued was cancelled or player disconnected.");
            }
            catch (Exception e) {
                Console.Error.WriteLine($"[DEBUG_LOG] Error during game execution: {e}");
            }
            finally {
                try {
                    game.Dispose();
                }
                catch (Exception e) {
                    Console.Error.WriteLine($"[DEBUG_LOG] Error during game disposal: {e}");
                }
            }
        }
    }
}
