using System.Collections.Concurrent;

namespace App;

public class ChessCollection {
    private ConcurrentDictionary<int, ChessAPI> _games = new();

    private int _idSeed = 7;
    public async Task<ChessMoveDTO> GetNextMove(ChessMoveDTO moveDto) {
        var game = _games[moveDto.GameId];
        return await game.GetBestMove(moveDto);
    }

    /// <summary>
    /// Creates a new game instance.
    /// </summary>
    /// <returns>Id of the game.</returns>
    public int GetNewGame() {
        int id = Interlocked.Increment(ref _idSeed);

        // should be safe, since id is safely unique
        _games[id] = new ChessAPI(id);
        
        return id;
    }
}