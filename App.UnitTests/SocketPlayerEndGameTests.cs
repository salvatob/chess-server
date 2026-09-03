using System.Net.WebSockets;
using System.Threading.Tasks;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Game;
using Xunit;

namespace App.UnitTests;

public class SocketPlayerEndGameTests {
    [Fact]
    public async Task GameEnd_SequenceOfOperations_IsCorrect() {
        // Arrange
        var mockSocket = new MockWebSocketWrapper();
        var player = new SocketPlayer(mockSocket);
        var result = new GameResult(GameOutcome.WhiteWin, GameEndReason.Checkmate, new List<Move>());

        // Act
        // 1. Trigger game end
        await player.OnGameEndAsync(true, result);
        // 2. Dispose the player (which should dispose the socket)
        player.Dispose();

        // Assert
        // We expect: SendMessageAsync (EndGameDto) -> CloseSocketAsync -> Dispose
        
        Assert.Equal(3, mockSocket.CallOrder.Count);
        Assert.Equal(nameof(IWebSocketWrapper.SendMessageAsync), mockSocket.CallOrder[0]);
        Assert.Equal(nameof(IWebSocketWrapper.CloseSocketAsync), mockSocket.CallOrder[1]);
        Assert.Equal(nameof(IWebSocketWrapper.Dispose), mockSocket.CallOrder[2]);

        // Verify EndGameDto was actually sent
        var message = Assert.Single(mockSocket.SentMessages);
        Assert.IsType<EndGameDto>(message);
        
        // Verify state
        Assert.Equal(WebSocketState.Closed, mockSocket.State);
        Assert.True(mockSocket.Disposed);
    }
    
    [Fact]
    public async Task OnGameEndAsync_AwaitsMessageAndHandshake() {
        // This test ensures that OnGameEndAsync itself awaits both operations
        // Arrange
        var mockSocket = new MockWebSocketWrapper();
        var player = new SocketPlayer(mockSocket);
        var result = new GameResult(GameOutcome.WhiteWin, GameEndReason.Checkmate, new List<Move>());

        // Act
        await player.OnGameEndAsync(true, result);

        // Assert
        // At this point, SendMessage and CloseSocket should have completed
        Assert.Contains(nameof(IWebSocketWrapper.SendMessageAsync), mockSocket.CallOrder);
        Assert.Contains(nameof(IWebSocketWrapper.CloseSocketAsync), mockSocket.CallOrder);
        
        // Ensure SendMessage happened BEFORE CloseSocket
        int sendIndex = mockSocket.CallOrder.IndexOf(nameof(IWebSocketWrapper.SendMessageAsync));
        int closeIndex = mockSocket.CallOrder.IndexOf(nameof(IWebSocketWrapper.CloseSocketAsync));
        
        Assert.True(sendIndex >= 0, "SendMessageAsync should have been called");
        Assert.True(closeIndex >= 0, "CloseSocketAsync should have been called");
        Assert.True(sendIndex < closeIndex, "SendMessageAsync should be called before CloseSocketAsync");
        
        // Dispose should NOT have happened yet if we haven't called player.Dispose()
        Assert.DoesNotContain(nameof(IWebSocketWrapper.Dispose), mockSocket.CallOrder);
    }
}
