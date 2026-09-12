# Developer Documentation


## Application Design

The application is a web-based chess platform that allows users to play against various opponents (Bots, Engines, or other Humans).
It is built using **ASP.NET Core** for the backend and a **vanilla JavaScript** frontend.
It is dependent on a **ChessBotCore** library for all Chess related functionality.


### High-Level Architecture
- **Web Layer**: Handles HTTP requests for game creation and upgrades connections to WebSockets for real-time gameplay.
- **Game Management Layer**: Orchestrates multiple concurrent games, managing their lifecycle from creation to completion.
- **Abstraction Layer (`IPlayer`)**: Decouples the game engine from the source of moves, allowing human players (via WebSockets), AI bots, and random movers to interact uniformly.
- **Engine Layer**: Utilizes an external `ChessBotCore` library for move validation, state management (FEN), and AI search.

## Core Algorithms & Patterns

### Multi-threaded Game Management
The `ChessManager` uses a **Producer-Consumer pattern** via `System.Threading.Channels`.
1. Games are prepared in a `GameBuilder`.
2. Once ready, they are enqueued into a `Channel<ChessGame>`.
3. A pool of `GameWorkerAsync` tasks (consumers) pick up games and execute the game loop (`PlayAsync`).

### The Game Loop
Each game runs its own asynchronous loop where it:
1. Asks the current player for a move via `ChooseMoveAsync`.
2. Validates and applies the move.
3. Notifies the opponent of the move.
4. Checks for end-game conditions.

## Communication & Data Flow

### User Input
1. **HTTP POST `/chess/create`**: The frontend sends game settings (time, FEN, opponent type). The server returns a `gameId`.
2. **WebSocket `/chess/ws/{id}`**: The browser connects to this endpoint. The server wraps this connection in a `SocketPlayer`.

### Database & State
Currently, the application is **stateless** in regards to a traditional database. Game state is kept in memory within the `ChessManager` and `ChessGame` objects. Board state is communicated using **FEN (Forsyth-Edwards Notation)**.

### Communication Protocol
JSON messages are exchanged over WebSockets. Key messages include:
- `PrepareGame`: Initial sync of board and clocks.
- `RequestMove`: Asking the client to provide a move.
- `OpponentMove`: Notifying the client of the other player's move.
- `EndGame`: Final result and reason.

## Extensibility Guide

### Adding a New Opponent Type
To add a new type of player (e.g., a neural-network-based bot):
1. Implement the `IPlayer` interface (found in `ChessBotCore.Players`).
2. Add the logic for choosing moves in `ChooseMoveAsync`.
3. Update `Main.cs` in the `RequestGameCreation` method to include your new player type in the `switch` statement.

### Modifying the Frontend
The frontend logic resides in `App/wwwroot/chess/js/app.js`. It uses `chessboardjs` for visual representation and `chess.js` for local move validation and FEN parsing.

