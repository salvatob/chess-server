using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ChessBotCore;
using ChessBotCore.Players;
using ConsoleInterface;
namespace App;

public class ChessGameHandler {
    private ChessGame _game;
    private int _id;
    
    public ChessGameHandler(int id) {
        _id = id;
        _game = new(new EnginePlayer(), new EnginePlayer());
    }
    

}