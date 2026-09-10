using System.Net.WebSockets;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Board;
using ChessBotCore.Game;
using Xunit;

namespace App.UnitTests;

public class SocketPlayerErrorTests {
    private readonly MockWebSocketWrapper _mockSocket;
    private readonly SocketPlayer _socketPlayer;

    public SocketPlayerErrorTests() {
        _mockSocket = new MockWebSocketWrapper();
        _socketPlayer = new SocketPlayer(_mockSocket);
    }

    [Fact]
    public async Task ChooseMoveAsync_Throws_WhenInvalidCoordinatesSent() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers();
        
        // "z9" is invalid
        _mockSocket.PushMessage(new MoveDtoMessage { Move = new IncomingMoveDto { From = "z9", To = "e4" } });
        
        // Act & Assert
        using var handle = _socketPlayer.ChooseMoveAsync(state, timers);
        await Assert.ThrowsAsync<ArgumentException>(async () => await handle.Result);
    }

    [Fact]
    public async Task ChooseMoveAsync_Throws_WhenMoveDataIsMissing() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers();
        
        _mockSocket.PushMessage(new MoveDtoMessage { Move = null });
        
        // Act & Assert
        using var handle = _socketPlayer.ChooseMoveAsync(state, timers);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handle.Result);
    }

    [Fact]
    public async Task ChooseMoveAsync_Throws_WhenSocketClosesDuringWait() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers();
        
        // Act
        using var handle = _socketPlayer.ChooseMoveAsync(state, timers);
        
        // Wait for it to hit WaitMessageAsync
        await Task.Delay(50);
        
        // Simulate socket error
        var socketException = new Exception("Socket connection lost");
        _mockSocket.TriggerWaitException(socketException);
        
        // Assert
        var ex = await Assert.ThrowsAsync<Exception>(async () => await handle.Result);
        Assert.Equal("Socket connection lost", ex.Message);
    }
}
