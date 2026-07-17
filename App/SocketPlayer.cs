using System.Net.WebSockets;
using ChessBotCore;
using ChessBotCore.Players;

namespace App;

// TODO(#2)
public class SocketPlayer : IPlayer {
    private readonly WebSocket _socket;
    
    public SocketPlayer(WebSocket socket) {
        _socket = socket;
    }
    
    public SearchHandle GetBestMove(State state, Timers timers) {
        throw new NotImplementedException();
    }
    
}
