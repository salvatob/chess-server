# Developer Documentation

This document provides an overview of the web-based chess platform's architecture, core types, and communication protocols.

## High-Level Architecture

The application is built using **ASP.NET Core** for the backend and **vanilla JavaScript** for the frontend. Real-time gameplay is facilitated by WebSockets.

- **Web Layer**: Handles HTTP requests for game creation (`/chess/create`) and upgrades connections to WebSockets (`/chess/ws/{id}`) for gameplay.
- **Game Management Layer**: Orchestrates concurrent games using a Producer-Consumer pattern.
- **Player Abstraction**: A unified `IPlayer` interface allows the game engine to treat Humans (via WebSockets), Bots, and Engines uniformly.

## Core Types

### `ChessManager`
The central authority for managing the lifecycle of all games. It is fully thread-safe and manages a pool of `GameWorkerAsync` tasks that execute game loops.

- **Responsibilities**: Creating game builders, registering players, starting games, and managing the game queue.
- **Concurrency**: Uses `System.Threading.Channels` to distribute games to worker tasks.

### `GameBuilder`
A staging object used to configure a game before it starts, while preventing data races common in these opeartions.

- **Responsibilities**: Storing time controls, initial FEN, and registering the two `IPlayer` instances.
- **Lifecycle**: Once `TryBuild` is called, the `ChessGame` is created, and the builder is disposed.

### `SocketPlayer`
An implementation of `IPlayer` that acts as a bridge between the server-side game loop and a client connected via WebSocket.

- **Responsibilities**: Translating game events (like "opponent moved") into JSON messages for the frontend, and waiting for the user's move input via the socket.

## Code Example: Starting a Game

The following example demonstrates how to use the `ChessManager` and `GameBuilder` flow to initialize a game between a human (via WebSocket) and a bot.

```csharp
// 1. Create a new game entry (returns a unique ID)
int gameId = chessManager.CreateGame(
    whiteTime: TimeSpan.FromMinutes(10), 
    blackTime: TimeSpan.FromMinutes(10), 
    increment: TimeSpan.FromSeconds(5)
);

// 2. Register players to the game builder
// SocketPlayer is typically created in the WebSocket middleware/controller
var whitePlayer = new SocketPlayer(webSocket);
var blackPlayer = new RandomPlayer();

chessManager.RegisterPlayer(gameId, whitePlayer, white: true);
chessManager.RegisterPlayer(gameId, blackPlayer, white: false);

// 3. Start the game (enqueues it for a background worker)
chessManager.StartGame(gameId);
```

## WebSocket Message Protocol

The communication between the `SocketPlayer` and the frontend relies on a JSON-based messaging protocol. 

### Design Idea
The protocol is **asynchronous and event-driven**. The server drives the game state, sending notifications to the client when it's their turn or when an event occurs. The client responds to specific requests (like `RequestMove`) or receives passive updates (like `OpponentMove`).

> [!IMPORTANT]  
> The message protocol is currently in active development and is subject to change.

### Supported Messages

#### Server to Client (Outgoing)
- `PrepareGameDto`: Sent when the player is registered, containing initial settings.
- `GameStartedDto`: Sent when the game officially begins.
- `StartGameDto`: A comprehensive start message including initial state and timers.
- `RequestMoveDto`: Signals the client that it is their turn to move.
- `OpponentMoveDto`: Notifies the client of the move made by the opponent.
- `EndGameDto`: Sent when the game concludes (win, loss, draw, or error).
- `ErrorMessageDto`: Sent when an error occurs during gameplay.

#### Client to Server (Incoming)
- `MoveDtoMessage`: Sent by the client in response to a move request, containing the chosen move.
- `CreateGameDto`: (HTTP) Used to define initial game settings during the creation phase.

## Design Principles

Most of the code was implmeneted with thread safety in mind. I also paid a lot of attention to correct handling of ownership of `IDisposable` resources, such as the `WebSocket` object, whose lifetime is a very complex matter.
