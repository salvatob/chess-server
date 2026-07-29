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


    public ChessManager(int maxConcurrentGames = 1) {
        for (int i = 0; i < maxConcurrentGames; i++) {
            _ = Task.Run(GameWorkerAsync);
        }
    }
    
    
    /// <summary>
    /// Registers a game builder object in internal storage. Returns the id to that builder.
    /// </summary>
    /// <returns>The id number of the game created.</returns>
    public int CreateGame(TimeSpan whiteTime, TimeSpan blackTime, TimeSpan increment) {
        int id = Interlocked.Increment(ref _idSeed);
        // We don't create the ChessGame yet, because we need players.
        // Or we could store a placeholder.
        _gameBuilders[id] = new GameBuilder {
            Timers = new Timers {
                BaseWhiteTime = whiteTime,
                BaseBlackTime = blackTime,
                Increment = increment,
                WhiteTime = whiteTime,
                BlackTime = blackTime
            }
        };
        return id;
    }

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

    private void BuildGame(int builderId) {
        var builder = _gameBuilders[builderId];
        var game = builder.Build();
        if (_gameQueue.Writer.TryWrite(game))
            _gameBuilders.Remove(builderId, out _);
    }


    private async Task GameWorkerAsync() {
        await foreach (var game in _gameQueue.Reader.ReadAllAsync()) {
            try {
                await game.Play();

            }
            catch (Exception e) {
                Console.Error.WriteLine(e);
            }
            game.Dispose();
        }
    } 
}