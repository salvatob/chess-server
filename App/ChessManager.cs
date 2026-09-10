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
    /// Registers a game builder object in internal storage. Returns the id to that builder.
    /// </summary>
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

    /// <summary>
    /// Constructs a <see cref="ChessGame"/> from a registered builder and adds it to the game queue.
    /// </summary>
    /// <param name="builderId">The ID of the game builder to use.</param>
    /// <exception cref="InvalidOperationException">When the game is not ready.</exception>
    /// <exception cref="KeyNotFoundException">When the builder is not found.</exception>
    public void StartGame(int builderId) {
        if (!_gameBuilders.TryRemove(builderId, out var builder)) {
            // The builder has already timed out, been build, or just never existed.
            throw new KeyNotFoundException($"A builder with id {builderId} was not found.");
        }
        
        if (!builder.Ready) {
            // we just return the builder in the dict
            _gameBuilders[builderId] = builder;
        
            // the previous timeout removal could be called here, while the builder is taken out
            // so I opt to call it again here, so that the removal is ensured 'sometime'
            _ = RemoveBuilderAfterTimeout(builderId);
            throw new InvalidOperationException("Players are not set yet.");
        }
        
        ChessGame game = builder.Build();
            
        builder.Dispose();
        // forcing builder.Dispose() is mostly for peace of mind, since ownership of all data
        // has been transferred out of the builder
        
         if (!_gameQueue.Writer.TryWrite(game)) {
             game.Dispose();
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
    /// A background worker that processes and plays games from the game queue.
    /// </summary>
    /// <returns>A task representing the worker process.</returns>
    private async Task GameWorkerAsync() {
        await foreach (var game in _gameQueue.Reader.ReadAllAsync()) {
            try {
                await game.PlayAsync();
            }
            catch (Exception e) {
                Console.Error.WriteLine(e);
            }
            finally {
                try {
                    game.Dispose();
                }
                catch (Exception e) {
                    Console.Error.WriteLine(e);
                }
            }
        }
    } 
}
