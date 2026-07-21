using System.Text.Json.Serialization;

namespace App.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartGameDto), "StartGame")]
[JsonDerivedType(typeof(RequestMoveDto), "RequestMove")]
[JsonDerivedType(typeof(EndGameDto), "EndGame")]
[JsonDerivedType(typeof(MoveDto), "Move")]
public abstract class SocketMessage;

public class StartGameDto : SocketMessage {
    public string Color { get; set; } = "";
    public string InitialFen { get; set; } = "";
}

public class RequestMoveDto : SocketMessage {
    public string State { get; set; } = "";
    public string Timers { get; set; } = "";
}

public class EndGameDto : SocketMessage {
    public string Result { get; set; } = "";
    public string? Reason { get; set; }
}

public class MoveDto : SocketMessage {
    public string Move { get; set; } = "";
}
