using ChessBotCore;
using ChessBotCore.Game;
using ChessBotCore.Players;
using Xunit;

namespace App.UnitTests;

public class ChessManagerTests {
    private const string MateInOneFen = "4k3/8/4K3/8/8/8/8/4Q3 w - - 0 1";

    private async Task<(int id, MockPlayer white, MockPlayer black)> SetupGame(ChessManager manager, string? fen = null) {
        var white = new MockPlayer();
        var black = new MockPlayer();
        int id = manager.CreateGame(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2), fen);
        manager.RegisterPlayer(id, white, true);
        manager.RegisterPlayer(id, black, false);
        return (id, white, black);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CreateGame_And_RegisterPlayers_StartsAndFinishesGame(int maxConcurrent) {
        // Arrange
        var manager = new ChessManager(maxConcurrentGames: maxConcurrent);
        
        // Act
        // Use a FEN that is almost mate so the game ends quickly
        var (id, white, black) = await SetupGame(manager, MateInOneFen);

        manager.StartGame(id);
        
        // Assert
        // The game should finish very quickly because the MockPlayer will play the winning move
        // or just some move that leads to end. 
        // Actually MockPlayer plays whatever move is first in legal moves.
        
        // Let's give it a bit of time
        await Task.Delay(500);

        Assert.True(white.Disposed);
        Assert.True(black.Disposed);
    }

    [Fact]
    public async Task MaxConcurrentGames_IsRespected() {
        // Arrange
        var manager = new ChessManager(maxConcurrentGames: 1);
        var stuckPlayer = new BlockingPlayer();
        
        // Game 1: stuck
        var whitePlayer1 = stuckPlayer;
        var blackPlayer1 = new MockPlayer();
        int id1 = manager.CreateGame(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), TimeSpan.Zero);
        manager.RegisterPlayer(id1, whitePlayer1, true);
        manager.RegisterPlayer(id1, blackPlayer1, false);
        
        await Task.Delay(200); 
        
        // Game 2: should be queued
        var (id, white2, _) = await SetupGame(manager);
        
        manager.StartGame(id);
        await Task.Delay(500);
        
        // Assert
        Assert.False(white2.Disposed); 
        
        // Act: Release Game 1
        stuckPlayer.Release();
        await Task.Delay(500);
        
        // Assert: Now Game 2 should have finished
        Assert.True(white2.Disposed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GameWorker_HandlesExceptionsGracefully(bool throwingIsWhite) {
        // Arrange
        var manager = new ChessManager(maxConcurrentGames: 1);
        var throwingPlayer = new ThrowingPlayer();
        var normalPlayer = new MockPlayer();
        
        int id = manager.CreateGame(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), TimeSpan.Zero);
        
        // Act
        if (throwingIsWhite) {
            manager.RegisterPlayer(id, throwingPlayer, true);
            manager.RegisterPlayer(id, normalPlayer, false);
        } else {
            manager.RegisterPlayer(id, normalPlayer, true);
            manager.RegisterPlayer(id, throwingPlayer, false);
        }
        
        manager.StartGame(id);
        
        await Task.Delay(500);
        
        // Assert
        Assert.True(throwingPlayer.Disposed);
        
        // Verify worker is still alive
        var (_, w2, _) = await SetupGame(manager, MateInOneFen);
        await Task.Delay(500);
        Assert.True(w2.Disposed);
    }

}
