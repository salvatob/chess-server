using System.Net.WebSockets;
using System.Text.Json;
using App;
using App.Dtos;
using ChessBotCore;
using ChessBotCore.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

internal class Program {
    /// <summary>
    /// Configures the web application and starts the server.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    public static void Main(string[] args) {

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions());
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        // builder.WebHost.UseUrls("http://192.168.1.1:5000");
        // builder.WebHost.UseUrls("http://192.168.1.226:5000");

        builder.Services.AddSingleton<ChessManager>();

        WebApplication app = builder.Build();

        app.UseWebSockets();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/", () => Results.Redirect($"/chess/selection.html", permanent: false));

        RouteGroupBuilder chess = app.MapGroup("/chess");


        chess.MapPost("/create", RequestGameCreation);

        chess.MapGet("/ws/{id:int}", RegisterWebSocketAsync);

        app.Run();

    }

    /// <summary>
    /// Handles the HTTP POST request to create a new chess game.
    /// </summary>
    /// <param name="dto">The game creation data transfer object.</param>
    /// <param name="manager">The chess manager instance.</param>
    /// <returns>A task representing the operation, returning the created game's ID.</returns>
    private static async Task<IResult> RequestGameCreation(CreateGameDto dto, ChessManager manager)  {
        int id = manager.CreateGame(
            TimeSpan.FromMilliseconds(dto.WhiteTimeMs),
            TimeSpan.FromMilliseconds(dto.BlackTimeMs),
            TimeSpan.FromMilliseconds(dto.IncrementMs),
            dto.Fen);

        IPlayer opponent = dto.Opponent.ToLower() switch {
            "engine" => new EnginePlayer(),
            "bot" => new EnginePlayer(),
            "random" => new RandomPlayer(),
            _ => throw new InvalidOperationException($"Unknown opponent: {dto.Opponent}")
        };

        bool playerIsWhite = dto.WhiteSide;

        manager.RegisterPlayer(id, opponent, !playerIsWhite);


        return TypedResults.Ok(new { Id = id });
    }
    
    /// <summary>
    /// Handles a WebSocket connection request for a specific game and player side.
    /// </summary>
    /// <param name="id">The ID of the game to join.</param>
    /// <param name="whiteSide">Whether the connecting player is white.</param>
    /// <param name="context">The HTTP context for the request.</param>
    /// <param name="manager">The chess manager instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task RegisterWebSocketAsync(int id, bool whiteSide, HttpContext context, ChessManager manager) {
        if (context.WebSockets.IsWebSocketRequest) {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine($"[DEBUG_LOG] New connection from {context.Connection.RemoteIpAddress}");
 
            // websocket ownership is transferred to the player
            var wsPLayer = new SocketPlayer(webSocket);
            manager.RegisterPlayer(id, wsPLayer, whiteSide);

            // Wait until the player signals the socket is closed
            await wsPLayer.WaitForCloseAsync();
            
            // Give a tiny bit of time for the network stack to flush the last frames 
            // before the request handler returns and potentially tears down the context.
            await Task.Delay(100);
            Console.WriteLine($"[DEBUG_LOG] Connection from {context.Connection.RemoteIpAddress} closing.");
        }
        else {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
