using System.Net.WebSockets;
using System.Threading.Tasks;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Board;
using ChessBotCore.Game;
using Xunit;

namespace App.UnitTests;

public class SocketPlayerTests {
    private readonly MockWebSocketWrapper _mockSocket;
    private readonly SocketPlayer _player;

    public SocketPlayerTests() {
        _mockSocket = new MockWebSocketWrapper();
        _player = new SocketPlayer(_mockSocket);
    }

    [Fact]
    public async Task OnGameStartAsync_SendsStartGameDto() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers { 
            BaseWhiteTime = TimeSpan.FromMilliseconds(1000), 
            BaseBlackTime = TimeSpan.FromMilliseconds(2000), 
            Increment = TimeSpan.FromMilliseconds(500) 
        };
        // ReSharper disable once CollectionNeverUpdated.Local
        var moveHistory = new List<Move>();

        // Act
        await _player.OnGameStartAsync(true, state, timers, moveHistory);

        // Assert
        var message = Assert.Single(_mockSocket.SentMessages);
        var startGameDto = Assert.IsType<StartGameDto>(message);
        Assert.True(startGameDto.ColorWhite);
        Assert.Equal(state.GetFen(), startGameDto.InitialFen);
        Assert.Equal(1000L, startGameDto.WhiteTimeMs);
        Assert.Equal(2000L, startGameDto.BlackTimeMs);
        Assert.Equal(500L, startGameDto.IncrementMs);
    }

    [Fact]
    public async Task PrepareAsync_SendsPrepareGameDto() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers { 
            BaseWhiteTime = TimeSpan.FromMilliseconds(3000), 
            BaseBlackTime = TimeSpan.FromMilliseconds(4000),
            Increment = TimeSpan.Zero
        };
        // ReSharper disable once CollectionNeverUpdated.Local
        var moveHistory = new List<Move>();

        // Act
        await _player.PrepareAsync(false, state, timers, moveHistory);

        // Assert
        var message = Assert.Single(_mockSocket.SentMessages);
        var prepareGameDto = Assert.IsType<PrepareGameDto>(message);
        Assert.False(prepareGameDto.ColorWhite);
        Assert.Equal(state.GetFen(), prepareGameDto.InitialFen);
        Assert.Equal(3000L, prepareGameDto.WhiteTimeMs);
        Assert.Equal(4000L, prepareGameDto.BlackTimeMs);
    }

    [Fact]
    public async Task ChooseMoveAsync_SendsRequestMoveAndReturnsBestMove() {
        // Arrange
        var state = State.Initial;
        var timers = new Timers { 
            BaseWhiteTime = TimeSpan.FromMilliseconds(5000), 
            BaseBlackTime = TimeSpan.FromMilliseconds(6000),
            Increment = TimeSpan.Zero
        };
        
        var moveDto = new MoveDtoMessage {
            Move = new IncomingMoveDto {
                From = "e2",
                To = "e4"
            }
        };
        _mockSocket.IncomingMessages.Enqueue(moveDto);

        // Act
        using var handle = _player.ChooseMoveAsync(state, timers);
        var results = await handle.Result;

        // Assert
        // 1. Check RequestMoveDto was sent
        var message = Assert.Single(_mockSocket.SentMessages);
        var requestMoveDto = Assert.IsType<RequestMoveDto>(message);
        Assert.Equal(state.GetFen(), requestMoveDto.Fen);
        Assert.Equal(5000L, requestMoveDto.WhiteTimeMs);
        Assert.Equal(6000L, requestMoveDto.BlackTimeMs);

        // 2. Check returned move
        Assert.Equal("e2e4", results.BestMove.PrintLAN());
    }

    [Fact]
    public async Task OnGameEndAsync_SendsEndGameDtoAndClosesSocket() {
        // Arrange
        var result = new GameResult(GameOutcome.WhiteWin, GameEndReason.Checkmate, new List<Move>());

        // Act
        await _player.OnGameEndAsync(true, result);

        // Assert
        var message = Assert.Single(_mockSocket.SentMessages);
        var endGameDto = Assert.IsType<EndGameDto>(message);
        Assert.Equal(result, endGameDto.Result);
        Assert.Equal(nameof(GameEndReason.Checkmate), endGameDto.Reason);
        Assert.Equal(WebSocketState.Closed, _mockSocket.State);
    }

    [Fact]
    public async Task OnOpponentsMoveAsync_SendsOpponentMoveDto() {
        // Arrange
        var state = State.Initial;
        // e2e4 move
        var move = Move.FindFullMove(
            new MoveDTO(
                Coordinates.FromString("e2").To1D(),
                Coordinates.FromString("e4").To1D(),
                null),
            state
            );
        
        var newState = state.Clone();
        newState.ApplyMove(move);

        // Act
        await _player.OnOpponentsMoveAsync(move, newState);

        // Assert
        var message = Assert.Single(_mockSocket.SentMessages);
        var opponentMoveDto = Assert.IsType<OpponentMoveDto>(message);
        Assert.Equal("e2e4", opponentMoveDto.MoveLAN);
        Assert.Equal(newState.GetFen(), opponentMoveDto.FenAfter);
    }

    [Fact]
    public async Task OnErrorNotifyAsync_SendsErrorMessageDto() {
        // Arrange
        var errorMessage = "Something went wrong";
        var isGameEnd = true;

        // Act
        await _player.OnErrorNotifyAsync(errorMessage, isGameEnd);

        // Assert
        var message = Assert.Single(_mockSocket.SentMessages);
        var errorDto = Assert.IsType<ErrorMessageDto>(message);
        Assert.Equal(errorMessage, errorDto.Message);
        Assert.True(errorDto.GameEnd);
    }
}
