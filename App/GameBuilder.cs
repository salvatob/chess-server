using System.Diagnostics;
using ChessBotCore;
using ChessBotCore.Game;

namespace App;

/// <summary>
/// A simple builder class for a <seealso cref="ChessGame"/> object.
/// </summary>
public class GameBuilder {
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

    /// <summary>
    /// Builds the <seealso cref="ChessGame"/> object.
    /// </summary>
    /// <returns>The built <seealso cref="ChessGame"/> instance.</returns>
    /// <exception cref="InvalidOperationException">When the object is not ready to be built (some properties are missing.)</exception>
    public ChessGame Build() {
        if (!Ready) {
            throw new InvalidOperationException("Players are not set yet.");
        }
        return new ChessGame(WhitePlayer!, BlackPlayer!, Timers, State);
    }
}
