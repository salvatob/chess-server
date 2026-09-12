using App;
using ChessBotCore;
using Xunit;

namespace App.UnitTests;

public class GameBuilderTests {
    [Fact]
    public void Build_Throws_WhenPlayersAreMissing() {
        // Arrange
        var builder = new GameBuilder();
        
        // Act & Assert
        Assert.False(builder.TryBuild(out _));
    }

    [Fact]
    public void Build_Throws_WhenOnlyWhitePlayerSet() {
        // Arrange
        var builder = new GameBuilder();
        builder.WhitePlayer = new MockPlayer();
        
        // Act & Assert
        Assert.False(builder.TryBuild(out _));
    }

    [Fact]
    public void Build_Succeeds_WhenBothPlayersSet() {
        // Arrange
        var builder = new GameBuilder();
        builder.WhitePlayer = new MockPlayer();
        builder.BlackPlayer = new MockPlayer();
        
        // Act
        var success = builder.TryBuild(out var game);
        
        // Assert
        Assert.True(success);
        Assert.NotNull(game);
    }
}
