# WebSocket Game Architecture Development Plan

## Goal

Separate the responsibilities of:

- **GameManager** - Owns and manages game instances.
- **SocketPlayer** - Represents a single connected client through a WebSocket.
- **Game Initiation Endpoint** - Accepts incoming connections and wires everything together.

The `Game` itself remains transport-agnostic and communicates only through the `IPlayer` interface.

---

# 1. GameManager

## Responsibilities

- Own all active `Game` instances.
- Create new games from HTTP requests.
- Store games in an internal collection (e.g. `Dictionary<GameId, Game>`).
- Register newly connected players with existing games.
- Start a game automatically once all required players have connected.
- Remove completed games from the collection.

## Public Operations

- `CreateGame(...)`
- `AttachPlayer(gameId/token, SocketPlayer)`
- `RemoveGame(...)`

The `GameManager` is the authority over game lifetime.

---

# 2. SocketPlayer

## Responsibilities

Represents a single WebSocket connection as an implementation of `IPlayer`.

Owns:

- the `WebSocket`
- receiving incoming messages
- sending game events to the client

Provides moves to the `Game` through the existing `IPlayer` interface.

The `Game` never communicates with the WebSocket directly.

---

# 3. Game / SocketPlayer Initiation Endpoint

## Responsibilities

The endpoint is responsible only for establishing the connection.

Typical flow:

1. Receive WebSocket request.
2. Validate the supplied game identifier or connection token.
3. Accept the WebSocket.
4. Create a new `SocketPlayer`.
5. Register it with the `GameManager`.
6. Await the lifetime of the connection.
7. Perform any final cleanup after disconnection.

The endpoint never creates games and contains no chess logic.

---

# Relationships

```text
                HTTP

Client
    │
    ▼
Create Game Request
    │
    ▼
GameManager
    │
    ▼
 Game (stored)

────────────────────────────────────────────

             WebSocket

Client
    │
    ▼
Connection Endpoint
    │
    ▼
SocketPlayer
    │
    ▼
GameManager.AttachPlayer(...)
    │
    ▼
Game
   ├── White : IPlayer
   └── Black : IPlayer
```

Once both players have been attached, the `GameManager` starts the game.

The `Game` communicates only through `IPlayer`, making it independent of the underlying transport and allowing different player implementations (WebSocket, AI, console, etc.) to participate interchangeably.