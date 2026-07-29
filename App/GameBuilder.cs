using System.Diagnostics;
using ChessBotCore;
using ChessBotCore.Game;

namespace App;

public class GameBuilder {
    public IPlayer? WhitePlayer { private get; set; }
    public IPlayer? BlackPlayer { private get; set; }
    public bool WhiteIsSet => WhitePlayer != null;
    public bool BlackIsSet => BlackPlayer != null;
    public Timers Timers  { get; set; } =  new Timers();
    public State State { get; set; } = State.Initial;
    
    public bool Ready => WhiteIsSet && BlackIsSet;

    public ChessGame Build() {
        if (!Ready) {
            throw new InvalidOperationException("Players are not set yet.");
        }
        return new ChessGame(WhitePlayer!, BlackPlayer!, Timers, State);
    }
}