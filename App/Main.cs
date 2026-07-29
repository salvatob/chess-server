using System.Net.WebSockets;
using System.Text.Json;
using App;
using App.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

internal class Program {
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
        // app.MapGet("/", () => Results.Redirect($"/number_adder", permanent: false));

        RouteGroupBuilder numberAdder = app.MapGroup("/number_adder");

        numberAdder.MapGet("/{id:int}", (int id) => {
                Console.WriteLine($"User has sent a number adder of int {id}");
                return TypedResults.Ok(1000 - id);
            }
        );

        RouteGroupBuilder chess = app.MapGroup("/chess");


        chess.MapPost("/create", (CreateGameDto dto, ChessManager manager) => {
            int id = manager.CreateGame(
                TimeSpan.FromMilliseconds(dto.WhiteTimeMs),
                TimeSpan.FromMilliseconds(dto.BlackTimeMs),
                TimeSpan.FromMilliseconds(dto.IncrementMs));
            
            if (dto.Opponent.Equals("bot", StringComparison.OrdinalIgnoreCase)) {
                bool playerIsWhite = dto.Side.Equals("white", StringComparison.OrdinalIgnoreCase);
                manager.RegisterPlayer(id, new ChessBotCore.Players.EnginePlayer(), !playerIsWhite);
            }
            
            return TypedResults.Ok(new { Id = id });
        });

        chess.MapGet("/ws/{id:int}", RegisterWebSocketAsync);

        app.Run();

    }

    private static async Task RegisterWebSocketAsync(int id, string side, HttpContext context, ChessManager manager) {
        if (context.WebSockets.IsWebSocketRequest) {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            // websocket ownership is transferred to the player
            var wsPLayer = new SocketPlayer(webSocket);
            bool white = "white".Equals(side, StringComparison.OrdinalIgnoreCase);
            manager.RegisterPlayer(id, wsPLayer, white: white);
        }
        else {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}