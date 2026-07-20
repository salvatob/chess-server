using System.Collections.Concurrent;
using ConsoleInterface;

namespace App;

/// <summary>
/// Class responsible for managing all chess games. Naturally is fully thread safe.
/// </summary>
public class ChessManager {
    private ConcurrentDictionary<int, ChessGame> _games = new();

    
    
}