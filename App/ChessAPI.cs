using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ChessBotCore;

namespace App;

public class ChessMoveDTO {
    public required int GameId { get; init; }
    public required long WhiteMs { get; set; }
    public required long BlackMs { get; set; }
    public string? FenBefore { get; init; }
    public string? FenAfter { get; set; }
}

public class ChessAPI {
    public required int Id { get; init; }
    // public long WhiteMs { get; set; }
    // public long BlackMs { get; set; }
    // public  State CurrentState { get; set; }
    private IChessWrapper _engine;

    [SetsRequiredMembers]
    public ChessAPI(int id) {
        Id = id;
        _engine = new DefaultChessWrapper();
    }


    public async Task<ChessMoveDTO> GetBestMove(ChessMoveDTO moveDto) {
        ArgumentNullException.ThrowIfNull(moveDto.FenBefore, nameof(moveDto.FenBefore));
        
        var state = State.FromFen(moveDto.FenBefore);

        var sw = Stopwatch.StartNew();
        Move move = await _engine.GetBestMove(state, 10_000);
        sw.Stop();

        UpdateTime(moveDto, sw.ElapsedMilliseconds);

        moveDto.FenAfter = move.StateAfter.GetFen();
        return moveDto;
    }

    /// <summary>
    /// Updates active players time.
    /// </summary>
    /// <param name="moveDto">The move object to mutate. The <see cref="ChessMoveDTO.FenBefore"/> prop must not be null.</param>
    /// <param name="timeElapsed">Time to subtract from the active player.</param>
    /// <returns>The same DTO with time updated.</returns>
    private ChessMoveDTO UpdateTime(ChessMoveDTO moveDto, long timeElapsed) {
        bool activeColor = State.DetectActiveColor(moveDto.FenBefore!);
        if (activeColor)
            moveDto.WhiteMs -= timeElapsed;
        else
            moveDto.BlackMs -= timeElapsed;

        return moveDto;
    }
}