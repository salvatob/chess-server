using System.Diagnostics;
using ChessBotCore;
using ChessBotCore.Game;

namespace App;

/// <summary>
/// A simple builder class for a <seealso cref="ChessGame"/> object.
/// </summary>
public class GameBuilder {
    public IPlayer? WhitePlayer { private get; set; }
    public IPlayer? BlackPlayer { private get; set; }
    public bool WhiteIsSet => WhitePlayer != null;
    public bool BlackIsSet => BlackPlayer != null;
    public Timers Timers  { get; set; } =  new Timers();
    public State State { get; set; } = State.Initial;
    
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