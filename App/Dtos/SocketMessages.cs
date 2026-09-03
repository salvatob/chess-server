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

    /// <summary>
    /// Message sent to a player to notify them that the game has officially started.
    /// </summary>
    public class StartGameDto : OutgoingSocketMessage {
        /// <summary>
        /// Whether the player is playing as white.
        /// </summary>
        public required bool ColorWhite { get; set; }
        /// <summary>
        /// The initial board state in FEN format.
        /// </summary>
        public required string InitialFen { get; set; } = "";
        /// <summary>
        /// The time white has remaining in milliseconds.
        /// </summary>
        public required long WhiteTimeMs { get; set; }
        /// <summary>
        /// The time black has remaining in milliseconds.
        /// </summary>
        public required long BlackTimeMs { get; set; }
        /// <summary>
        /// The time increment per move in milliseconds.
        /// </summary>
        public required long IncrementMs { get; set; }
    }

    /// <summary>
    /// Message sent to a player to request they choose a move.
    /// </summary>
    public class RequestMoveDto : OutgoingSocketMessage {
        /// <summary>
        /// The current board state in FEN format.
        /// </summary>
        public required string Fen { get; set; }
        /// <summary>
        /// The time white has remaining in milliseconds.
        /// </summary>
        public required long WhiteTimeMs { get; set; }
        /// <summary>
        /// The time black has remaining in milliseconds.
        /// </summary>
        public required long BlackTimeMs { get; set; }
        /// <summary>
        /// The last move played in LAN format.
        /// </summary>
        public string? LastMoveLAN { get; set; }
    }

    /// <summary>
    /// Message sent to a player to notify them that the game has ended.
    /// </summary>
    public class EndGameDto : OutgoingSocketMessage {
        /// <summary>
        /// The final result of the game.
        /// </summary>
        public required GameResult Result { get; set; }
        /// <summary>
        /// A string representation of the reason the game ended.
        /// </summary>
        public required string Reason { get; set; }
    }

    /// <summary>
    /// Message sent to a player to prepare them for an upcoming game.
    /// </summary>
    public class PrepareGameDto : OutgoingSocketMessage {
        /// <summary>
        /// Whether the player will be playing as white.
        /// </summary>
        public required bool ColorWhite { get; set; }
        /// <summary>
        /// The initial board state in FEN format.
        /// </summary>
        public required string InitialFen { get; set; } = "";
        /// <summary>
        /// The time white will have remaining in milliseconds.
        /// </summary>
        public required long WhiteTimeMs { get; set; }
        /// <summary>
        /// The time black will have remaining in milliseconds.
        /// </summary>
        public required long BlackTimeMs { get; set; }
    }

    /// <summary>
    /// Message sent to a player to notify them of their opponent's move.
    /// </summary>
    public class OpponentMoveDto : OutgoingSocketMessage {
        /// <summary>
        /// The move the opponent played in LAN format.
        /// </summary>
        public required string MoveLAN { get; set; }
        /// <summary>
        /// The board state after the opponent's move in FEN format.
        /// </summary>
        public required string FenAfter { get; set; }
    }

    /// <summary>
    /// Message sent to a player to notify them that the game has started (simple signal).
    /// </summary>
    public class GameStartedDto : OutgoingSocketMessage {
        // No extra fields needed, just a signal
    }

    /// <summary>
    /// Message sent to a player to notify them of an error.
    /// </summary>
    public class ErrorMessageDto : OutgoingSocketMessage {
        /// <summary>
        /// The error message.
        /// </summary>
        public required string Message { get; set; }
        /// <summary>
        /// Whether the error is critical enough to end the game.
        /// </summary>
        public required bool GameEnd { get; set; }
    }

    /// <summary>
    /// Represents a chess move received from a player.
    /// </summary>
    public class IncomingMoveDto {
        /// <summary>
        /// The starting square of the move (e.g., "e2").
        /// </summary>
        public string From { get; set; } = "";
        /// <summary>
        /// The target square of the move (e.g., "e4").
        /// </summary>
        public string To { get; set; } = "";
        /// <summary>
        /// The piece type to promote to, if applicable.
        /// </summary>
        public char? Promotion { get; set; }
    }

    /// <summary>
    /// Message received from a player containing their chosen move.
    /// </summary>
    public class MoveDtoMessage : IncomingSocketMessage {
        /// <summary>
        /// The move chosen by the player.
        /// </summary>
        public IncomingMoveDto? Move { get; set; }
    }

    /// <summary>
    /// Data transfer object for creating a new game.
    /// </summary>
    public record class CreateGameDto {
        /// <summary>
        /// The base time for white in milliseconds.
        /// </summary>
        public required long WhiteTimeMs { get; set; }
        /// <summary>
        /// The base time for black in milliseconds.
        /// </summary>
        public required long BlackTimeMs { get; set; }
        /// <summary>
        /// The time increment per move in milliseconds.
        /// </summary>
        public required long IncrementMs { get; set; }
        /// <summary>
        /// The type of opponent (e.g., "engine", "random").
        /// </summary>
        public required string Opponent { get; set; } = "";
        /// <summary>
        /// Whether the player is playing as white.
        /// </summary>
        public required bool WhiteSide { get; set; }
        /// <summary>
        /// Optional initial board state in FEN format.
        /// </summary>
        public string? Fen { get; set; }
    }
