# WebSocket Player Support Development Plan

## Goal

Introduce network-based players using WebSockets while keeping the core game logic independent from networking.

The `Game` class should continue to communicate only through the `IPlayer` abstraction. It should not know whether a player is connected through a WebSocket, running locally, controlled by an AI, or using another communication method.

The feature consists of three main parts:

1. **GameManager** - manages active games and player registration.
2. **SocketPlayer** - represents a player connected through a WebSocket.
3. **Game initiation flow** - connects HTTP requests, WebSocket connections, players, and games.

---

# Progress

| Component | Status             |
|---|--------------------|
| GameManager implementation | Not started        |
| SocketPlayer implementation | First version done |
| WebSocket initiation endpoint | Not started        |
| Integration with Game loop | Not started        |
| Testing and validation | Not started        |

---

# Architecture Overview

The system consists of the following relationships:

```
                         HTTP

Client
  |
  | Create game request
  v
GameManager
  |
  | Creates and stores
  v
Game
  |
  |
  +----------------+
  |                |
IPlayer        IPlayer
  |                |
  v                v
Player A        Player B


                         WebSocket

Client
  |
  | Connect using game identifier/token
  v
WebSocket Endpoint
  |
  | Creates
  v
SocketPlayer
  |
  | Registers
  v
GameManager
  |
  v
Game
```

The `GameManager` owns game lifetime.

The `SocketPlayer` owns the WebSocket connection.

The `Game` owns the players and only interacts through `IPlayer`.

---

# 1. GameManager

## Responsibilities

The `GameManager` is responsible for managing active games.

It should:

- Create new game instances.
- Store active games internally.
- Find games based on identifiers/tokens.
- Register players into existing games.
- Start games when all required players are connected.
- Remove completed games.

## Expected Responsibilities

The GameManager should know:

- Which games exist.
- Which players belong to each game.
- When a game is ready to begin.

The GameManager should not know:

- How players communicate.
- Whether a player is human or AI.
- How the game itself is played.

## Testing

Verify:

- Multiple games can exist simultaneously.
- Players can be registered correctly.
- Games start only when all required players are present.
- Completed games are removed correctly.

---

# 2. SocketPlayer

## Responsibilities

`SocketPlayer` implements `IPlayer` and represents one connected client.

It owns:

- The WebSocket connection.
- Receiving messages from the client.
- Sending game events to the client.

The socket player uses a [simple, custom protocol](../protocols/chess-socket-protocol.md) 
for bidirectional communication between the client and the server. 

The SocketPlayer translates between:

```
Game commands/events
        |
        v
IPlayer interface
        |
        v
WebSocket messages
```

## Expected Behaviour

Incoming messages:

```
Client
  |
  | Move message
  v
SocketPlayer
  |
  v
IPlayer.GetMoveAsync()
```

Outgoing events:

```
Game
 |
 | Game started / Move played / Game ended
 v
SocketPlayer
 |
 v
Client
```

The SocketPlayer should hide all networking details from the game.

## Testing

Verify:

- Messages can be received correctly.
- Moves are delivered to the game.
- Game events are sent to the client.
- Connection closing is handled correctly.

---

# 3. Game Initiation Flow

## Responsibilities

The initiation flow connects HTTP, WebSockets, players, and games.

The process should be:

1. Client sends a normal HTTP request to create a game.

```
POST /games
```

2. Server creates a game through `GameManager`.

3. Server returns an identifier or connection token.

4. Client opens a WebSocket connection using that identifier.

```
ws://server/game/{token}
```

5. The WebSocket endpoint:

- validates the identifier/token,
- accepts the WebSocket,
- creates a `SocketPlayer`,
- registers it with the `GameManager`.

6. When all players are connected, the `GameManager` starts the game.

## Endpoint Responsibilities

The WebSocket endpoint should only:

- Handle the connection.
- Create the `SocketPlayer`.
- Register the player.
- Wait for connection termination.
- Perform cleanup.

It should not:

- Create games.
- Contain game logic.
- Handle moves directly.

---

# Development Steps

## Step 1 - Implement GameManager

Tasks:

- [X] Create internal storage for active games.
- [X] Implement game creation.
- [X] Implement player registration.
- [X] Implement automatic game start.
- [X] Implement cleanup of completed games.

---

## Step 2 - Implement SocketPlayer

Tasks:

- [X] Create WebSocket-backed `IPlayer`.
- [X] Implement receiving moves.
- [X] Implement sending game events.
- [X] Handle connection closing.

---

## Step 3 - Implement HTTP and WebSocket Flow

Tasks:

- [ ] Add HTTP endpoint for game creation.
- [ ] Return game identifier/token.
- [X] Add WebSocket endpoint.
- [ ] Validate incoming connections.
- [ ] Create and register SocketPlayers.

---

## Step 4 - Integrate With Game Loop

Tasks:

- [ ] Connect SocketPlayers to existing game logic.
- [ ] Run a complete game between two clients.
- [ ] Send final game results.
- [ ] Remove finished games.

---

# Testing Plan

## Unit Tests

Test individual components:

- GameManager creates and tracks games.
- SocketPlayer correctly processes messages.
- Invalid connections are rejected.

## Integration Tests

Test the whole flow:

- Client creates a game.
- Two clients connect.
- Both players join the same game.
- A complete game can be played.
- Final results are communicated.

## Manual Validation

Confirm:

- Multiple games can run at the same time.
- Two human players can play against each other.
- The game engine remains independent from networking.
- Existing non-networked `IPlayer` implementations still work.
