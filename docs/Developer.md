# Developer Documentation

## Application Design

The application is a web-based chess platform that allows users to play against various opponents (Bots, Engines, or other Humans). 
It is built using **ASP.NET Core** for the backend and a **vanilla JavaScript** frontend.

### High-Level Architecture
- **Web Layer**: Handles HTTP requests for game creation and upgrades connections to WebSockets for real-time gameplay.
- **Game Management Layer**: Orchestrates multiple concurrent games, managing their lifecycle from creation to completion via the `ChessManager`.
- **Endpoints**: There are currently two endpoints. One creates a new game, and returns an id for the socket connection. The other is the WebSocket, that handles the continuous connection between client and the server.
- **Socket Player**: A specialized implementation of `IPlayer` that bridges the game logic with a WebSocket connection, enabling real-time interaction with the frontend.

## Core classes

### Multi-threaded Game Management
The `ChessManager` uses a **Producer-Consumer pattern** via `System.Threading.Channels`.
1. Games are prepared using a `GameBuilder`.
2. Once both players are registered and the game is started, it is enqueued into a `Channel`.
3. A pool of `GameWorkerAsync` tasks (consumers) pick up games and execute the game loop (`PlayAsync`).

## ChessManager API Usage

The `ChessManager` is the central point for creating and starting games. Below is an example of how to use its API:

```csharp
// Create a game with default time controls
int gameId = chessManager.CreateGame();

Register players (e.g., a Human via WebSocket and a Bot)
// in reality, SocketPlayer is created from an endpoint, to which a client has already connected

var humanPlayer = new SocketPlayer(webSocket);
var rndPlayer = new RandomPlayer(); 

chessManager.RegisterPlayer(gameId, humanPlayer, white: true);
chessManager.RegisterPlayer(gameId, rndPlayer, white: false);

// Starting the game only enqueues it. It will be picked up by a worker task.
chessManager.StartGame(gameId);
```

## Communication & Data Flow

### User Input
1. **HTTP POST `/chess/create`**: The frontend sends game settings (time, FEN, opponent type). The server returns a `gameId`.
2. **WebSocket `/chess/ws/{id}`**: The browser connects to this endpoint. The server wraps this connection in a `SocketPlayer` and registers it to the game.

### Database & State
Currently, the application is **stateless** in regards to a traditional database. Game state is kept in memory within the `ChessManager` and `ChessGame` objects. Board state is communicated using **FEN (Forsyth-Edwards Notation)**.

### Communication Protocol
JSON messages are exchanged over WebSockets between the `SocketPlayer` and the frontend. Key messages include:
- `PrepareGame`: Initial sync of board and clocks.
- `RequestMove`: Asking the client to provide a move.
- `OpponentMove`: Notifying the client of the other player's move.
- `EndGame`: Final result and reason.

### Modifying the Frontend
The frontend logic resides in `App/wwwroot/chess/js/app.js`. It handles the WebSocket connection, renders the board, and sends user moves back to the server.

