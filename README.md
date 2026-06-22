# WebApp - Parallel Chess & Messenger

A multi-functional web application built with ASP.NET Core 9.0, featuring a parallelized chess engine, a real-time messenger, and utility tools.

## 🚀 Overview

This project is a combined credit project for NPRG035 and NPRG038. It consists of two main parts:
1.  **Parallel Chess Engine (`ChessBotCore`):** A C# chess bot designed for efficiency using multi-threading and parallel search algorithms.
2.  **Web Application:** A .NET Core Minimal API backend serving a frontend to play against the bot or communicate via a messenger.

## 🛠 Tech Stack

-   **Backend:** C# 13, .NET 9.0, ASP.NET Core Minimal APIs
-   **Database:** Entity Framework Core (In-Memory provider)
-   **Frontend:** Vanilla HTML5, CSS3, JavaScript (ES6+)
-   **Libraries:**
    -   `chessboard.js` (Chessboard UI)
    -   `chess.js` (Chess logic/validation)
    -   `jQuery` (Dependency for chessboard.js)

## 📋 Requirements

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   A modern web browser

## 🔧 Setup & Run

1.  **Clone the repository** (and ensure `ParallelChessBot` is in the expected relative path or update the project reference).
2.  **Restore dependencies:**
    ```powershell
    dotnet restore
    ```
3.  **Build the project:**
    ```powershell
    dotnet build
    ```
4.  **Run the application:**
    ```powershell
    dotnet run --project App
    ```
    The application will be available at `http://localhost:5000` by default.

## 📁 Project Structure

```text
.
├── App/
│   ├── Main.cs                 # Application entry point & API routing
│   ├── ChessAPI.cs              # Bridge between Web API and ChessBotCore
│   ├── ChessCollection.cs       # Game state management
│   ├── ConcurrentMessengerCollection.cs # Thread-safe message storage
│   ├── wwwroot/                 # Static files (Frontend)
│   │   ├── chess/               # Chess UI and logic
│   │   ├── messenger/           # Messenger UI and logic
│   │   └── number_adder/        # Demo utility
│   ├── appsettings.json         # Configuration
│   └── App.csproj               # Project dependencies
├── WebApp.sln                   # Solution file
├── global.json                  # SDK version pinning
└── README.md                    # You are here
```

## 📡 API Endpoints

The backend uses Minimal APIs organized into several groups:

### Chess (`/chess`)
-   `GET /chess/new-game`: Initializes a new game session.
-   `POST /chess/move`: Submits a move and receives the bot's response.

### Messenger (`/messenger`)
-   `GET /messenger/all`: Retrieves all messages.
-   `POST /messenger/new-message`: Sends a new message.
-   `GET /messenger/last-messages/{count}`: Gets the N most recent messages.
-   `GET /messenger/messages/{id}`: Gets a specific message by ID.

### Number Adder (`/number_adder`)
-   `GET /number_adder/{id}`: A simple demo endpoint.

## ⚙️ Environment Variables

-   `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`.
-   `PORT`: The application is configured to listen on `http://0.0.0.0:5000` by default in `Main.cs`.

## 🧪 Tests

-   **TODO:** Automated tests for `ChessBotCore` and API endpoints are yet to be fully integrated into the main solution.
-   Manual verification: Use the UI at `/chess` or `/messenger`.

## 📝 License

This is a student project created for academic purposes.
Existing vendor libraries (e.g., `chessboard.js`) carry their own respective licenses (see `wwwroot/chess/lib/chessboardjs/LICENSE.md`).

---
*Created by [Tobias](https://github.com/tobia) - 2026*
