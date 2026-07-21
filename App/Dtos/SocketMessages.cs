using System.Text.Json.Serialization;

namespace App.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartGameDto), "StartGame")]
[JsonDerivedType(typeof(RequestMoveDto), "RequestMove")]
[JsonDerivedType(typeof(EndGameDto), "EndGame")]
[JsonDerivedType(typeof(MoveDto), "Move")]
public abstract class SocketMessage { }

public class StartGameDto : SocketMessage {
    public string Color { get; set; } = string.Empty;
    public string InitialFen { get; set; } = string.Empty;
}

public class RequestMoveDto : SocketMessage { }

public class EndGameDto : SocketMessage {
    public string Result { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class MoveDto : SocketMessage {
    public string Move { get; set; } = string.Empty;
}
