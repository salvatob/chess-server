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
}

public class RequestMoveDto : SocketMessage {
    public string Fen { get; set; } = "";
    public TimeSpan WhiteTime { get; set; }
    public TimeSpan BlackTime { get; set; }
}

public class EndGameDto : SocketMessage {
    public GameResult? Result { get; set; }
    public string? Reason { get; set; }
}

public class MoveDtoMessage : SocketMessage {
    public MoveDTO? Move { get; set; }
}
