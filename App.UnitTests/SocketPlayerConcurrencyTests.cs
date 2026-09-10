using System.Net.WebSockets;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Board;
using ChessBotCore.Game;
using Xunit;

namespace App.UnitTests;

public class SocketPlayerConcurrencyTests {
    private readonly MockWebSocketWrapper _mockSocket;
    private readonly SocketPlayer _player;

    public SocketPlayerConcurrencyTests() {
        _mockSocket = new MockWebSocketWrapper();
        _player = new SocketPlayer(_mockSocket);
    }

    [Fact]
    public async Task ChooseMoveAsync_CanBeCancelled() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers();
        
        // Act
        using var handle = _player.ChooseMoveAsync(state, timers);
        
        // Ensure it's running
        await Task.Delay(50);
        
        handle.Cancel();
        
        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await handle.Result);
        Assert.Contains(nameof(IWebSocketWrapper.WaitMessageAsync), _mockSocket.CallOrder);
    }

    [Fact]
    public async Task ChooseMoveAsync_HandlesRapidMoves_Gracefully() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers();
        
        // Act
        var handle1 = _player.ChooseMoveAsync(state, timers);
        var handle2 = _player.ChooseMoveAsync(state, timers);
        
        // Give them a moment to start
        await Task.Delay(50);

        // Send move for handle 1
        _mockSocket.PushMessage(new MoveDtoMessage { Move = new IncomingMoveDto { From = "e2", To = "e4" } });
        var result1 = await handle1.Result;
        
        // Send move for handle 2
        _mockSocket.PushMessage(new MoveDtoMessage { Move = new IncomingMoveDto { From = "d2", To = "d4" } });
        var result2 = await handle2.Result;

        // Assert
        Assert.Equal("e2e4", result1.BestMove.PrintLAN());
        Assert.Equal("d2d4", result2.BestMove.PrintLAN());
        
        // Should have sent two RequestMoveDto
        Assert.Equal(2, _mockSocket.SentMessages.Count(m => m is RequestMoveDto));

        handle1.Dispose();
        handle2.Dispose();
    }
}
