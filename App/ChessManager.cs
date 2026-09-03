using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Channels;
using ChessBotCore;
using ChessBotCore.Game;
using ChessBotCore.Players;

namespace App;

/// <summary>
/// Class responsible for managing all chess games. Naturally is fully thread safe.
/// </summary>
public class ChessManager {
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
    /// <returns>The id number of the game created that can be used to further build the game.</returns>
    public int CreateGame(TimeSpan whiteTime, TimeSpan blackTime, TimeSpan increment, string? fen = null) {
        int id = Interlocked.Increment(ref _idSeed);
        // We don't create the ChessGame yet, because we need players.
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
        return id;
    }

    /// <summary>
    /// Registers an initialized player to a game builder.
    /// </summary>
    /// <param name="builderId">Id of the builder we are trying to </param>
    /// <param name="player">The player to register.</param>
    /// <param name="white">The color/side of the chess game we are trying to register the player to.</param>
    public void RegisterPlayer(int builderId, IPlayer player, bool white) {
        var gameBuilder = _gameBuilders[builderId];
        if (white) 
            gameBuilder.WhitePlayer = player;
        else
            gameBuilder.BlackPlayer = player;
        
        if (gameBuilder.Ready) {
            BuildGame(builderId);
        }
    }

    /// <summary>
    /// Constructs a <see cref="ChessGame"/> from a registered builder and adds it to the game queue.
    /// </summary>
    /// <param name="builderId">The ID of the game builder to use.</param>
    private void BuildGame(int builderId) {
        var builder = _gameBuilders[builderId];
        var game = builder.Build();
        if (_gameQueue.Writer.TryWrite(game))
            _gameBuilders.Remove(builderId, out _);
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
            // Give the players a brief moment to finish their own cleanup/socket closing
            // before the game and its players are disposed.
            // await Task.Delay(500); // TODO
            game.Dispose();
        }
    } 
}
