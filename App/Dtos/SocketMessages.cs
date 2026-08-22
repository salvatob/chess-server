using System.Text.Json.Serialization;
using ChessBotCore;
using ChessBotCore.Game;

namespace App.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartGameDto), "StartGame")]
[JsonDerivedType(typeof(RequestMoveDto), "RequestMove")]
[JsonDerivedType(typeof(EndGameDto), "EndGame")]
[JsonDerivedType(typeof(PrepareGameDto), "PrepareGame")]
[JsonDerivedType(typeof(OpponentMoveDto), "OpponentMove")]
[JsonDerivedType(typeof(GameStartedDto), "GameStarted")]
[JsonDerivedType(typeof(ErrorMessageDto), "ErrorMessage")]
public abstract class OutgoingSocketMessage;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveDtoMessage), "Move")]
public abstract class IncomingSocketMessage;

public class StartGameDto : OutgoingSocketMessage {
    public required bool ColorWhite { get; set; }
    public required string InitialFen { get; set; } = "";
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }
    public required long IncrementMs { get; set; }
}

public class RequestMoveDto : OutgoingSocketMessage {
    public required string Fen { get; set; }
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }
    public string? LastMoveLAN { get; set; }
}

public class EndGameDto : OutgoingSocketMessage {
    public required GameResult Result { get; set; }
    public required string Reason { get; set; }
}

public class PrepareGameDto : OutgoingSocketMessage {
    public required bool ColorWhite { get; set; }
    public required string InitialFen { get; set; } = "";
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }
}

public class OpponentMoveDto : OutgoingSocketMessage {
    public required string MoveLAN { get; set; }
    public required string FenAfter { get; set; }
}

public class GameStartedDto : OutgoingSocketMessage {
    // No extra fields needed, just a signal
}

public class ErrorMessageDto : OutgoingSocketMessage {
    public required string Message { get; set; }
    public required bool GameEnd { get; set; }
}

public class IncomingMoveDto {
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public char? Promotion { get; set; }
}

public class MoveDtoMessage : IncomingSocketMessage {
    public IncomingMoveDto? Move { get; set; }
}

// used for a different endpoint, that is why it isnt inheriting anything.
public record class CreateGameDto {
    public required long WhiteTimeMs { get; set; }
    public required long BlackTimeMs { get; set; }
    public required long IncrementMs { get; set; }
    public required string Opponent { get; set; } = "";
    public required bool WhiteSide { get; set; }
    public string? Fen { get; set; }
}
