# Chessbot Server

A simple web application built with ASP.NET Core 9.0, meant to showcase the chess engine to the public.


## 🛠 Tech Stack

-   **Backend:** C# 13, .NET 9.0, ASP.NET Core Minimal APIs
-   **Frontend:** HTML5, CSS3, JavaScript (ES6+)
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


## 📡 API Endpoints

The backend uses Minimal APIs organized into several groups:

### Chess (`/chess`)
-   `GET /chess/new-game`: Initializes a new game session.
-   `POST /chess/move`: Submits a move and receives the bot's response.

### Number Adder (`/number_adder`)
-   `GET /number_adder/{id}`: A simple demo endpoint.

## ⚙️ Environment Variables

-   `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`.
-   `PORT`: The application is configured to listen on `http://0.0.0.0:5000` by default in `Main.cs`.

## 🧪 Tests

-   **TODO:** Automated tests for `ChessBotCore` and API endpoints are yet to be fully integrated into the main solution.
-   Manual verification: Use the UI at `/chess`.

## 📝 License

This is a student project created for academic purposes.
Existing vendor libraries (e.g., `chessboard.js`) carry their own respective licenses (see `wwwroot/chess/lib/chessboardjs/LICENSE.md`).

