using System.Diagnostics;
using ChessBotCore;
using ChessBotCore.Game;

namespace App;

/// <summary>
/// A simple builder class for a <seealso cref="ChessGame"/> object.
/// </summary>
public class GameBuilder : IDisposable {
    /// <summary>
    /// The player playing as white.
    /// </summary>
    public IPlayer? WhitePlayer { private get; set; }

    /// <summary>
    /// The player playing as black.
    /// </summary>
    public IPlayer? BlackPlayer { private get; set; }

    /// <summary>
    /// Gets a value indicating whether the white player has been set.
    /// </summary>
    public bool WhiteIsSet => WhitePlayer != null;

    /// <summary>
    /// Gets a value indicating whether the black player has been set.
    /// </summary>
    public bool BlackIsSet => BlackPlayer != null;

    /// <summary>
    /// Gets or sets the timers for the game.
    /// </summary>
    public Timers Timers  { get; set; } =  new Timers();

    /// <summary>
    /// Gets or sets the initial state of the chess board.
    /// </summary>
    public State State { get; set; } = State.Initial;
    
    /// <summary>
    /// Gets a value indicating whether both players are set and the game is ready to be built.
    /// </summary>
    public bool Ready => WhiteIsSet && BlackIsSet;

    public bool TryBuild(out ChessGame game) {
        lock (this) {
            if (!Ready) {
                game = default;
                return false;
            }

            var newGame = new ChessGame(WhitePlayer!, BlackPlayer!, Timers, State);
        
            // delete refs so that disposal of this builder
            // does not dispose players used by the newly created game
            WhitePlayer = null;
            BlackPlayer = null;
            game = newGame;
            return true;
        }
    }
    
    public void Dispose() {
        WhitePlayer?.Dispose();
        BlackPlayer?.Dispose();
    }
}
