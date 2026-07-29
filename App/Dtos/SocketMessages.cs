using System.Text.Json.Serialization;
using ChessBotCore;
using ChessBotCore.Game;

namespace App.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartGameDto), "StartGame")]
[JsonDerivedType(typeof(RequestMoveDto), "RequestMove")]
[JsonDerivedType(typeof(EndGameDto), "EndGame")]
[JsonDerivedType(typeof(MoveDtoMessage), "Move")]
public abstract class SocketMessage;

public class StartGameDto : SocketMessage {
    public string Color { get; set; } = "";
    public string InitialFen { get; set; } = "";
    public TimeSpan WhiteTime { get; set; }
    public TimeSpan BlackTime { get; set; }
    public TimeSpan Increment { get; set; }
    public long WhiteTimeTotalMs => (long)WhiteTime.TotalMilliseconds;
    public long BlackTimeTotalMs => (long)BlackTime.TotalMilliseconds;
    public long IncrementTotalMs => (long)Increment.TotalMilliseconds;
}

public class RequestMoveDto : SocketMessage {
    public string Fen { get; set; } = "";
    public TimeSpan WhiteTime { get; set; }
    public TimeSpan BlackTime { get; set; }
    public long WhiteTimeTotalMs => (long)WhiteTime.TotalMilliseconds;
    public long BlackTimeTotalMs => (long)BlackTime.TotalMilliseconds;
}

public class EndGameDto : SocketMessage {
    public GameResult? Result { get; set; }
    public string? Reason { get; set; }
}

public class MoveDtoMessage : SocketMessage {
    public MoveDTO? Move { get; set; }
}

public class CreateGameDto {
    public long WhiteTimeMs { get; set; }
    public long BlackTimeMs { get; set; }
    public long IncrementMs { get; set; }
    public string Opponent { get; set; } = "";
    public string Side { get; set; } = "";
}
