using System.Text.Json.Serialization;
using ChessBotCore;
using ChessBotCore.Game;

namespace App.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartGameDto), "StartGame")]
[JsonDerivedType(typeof(RequestMoveDto), "RequestMove")]
[JsonDerivedType(typeof(EndGameDto), "EndGame")]
[JsonDerivedType(typeof(MoveDtoMessage), "Move")]
public abstract class IncomingSocketMessage;
public abstract class OutgoingSocketMessage;

public class StartGameDto : OutgoingSocketMessage {
    public required string Color { get; set; } = "";
    public required string InitialFen { get; set; } = "";
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }
    public required long IncrementMs { get; set; }
}

public class RequestMoveDto : OutgoingSocketMessage {
    public required string Fen { get; set; }
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }

}

public class EndGameDto : OutgoingSocketMessage {
    public required GameResult Result { get; set; }
    public required string Reason { get; set; }
}

public class MoveDtoMessage : IncomingSocketMessage {
    public MoveDTO? Move { get; set; }
}

// used for a different endpoint, that is why it isnt inheriting anything.
public record class CreateGameDto {
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }
    public required long IncrementMs { get; set; }
    public required string Opponent { get; set; } = "";
    public required string Side { get; set; } = "";
}
