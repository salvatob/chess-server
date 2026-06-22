using System.Text.Json;
using App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

internal class Program {
    public static void Main(string[] args) {
        
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions());
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        // builder.WebHost.UseUrls("http://192.168.1.1:5000");
        // builder.WebHost.UseUrls("http://192.168.1.226:5000");
        
        builder.Services.AddSingleton<ConcurrentMessengerCollection>();
        builder.Services.AddSingleton<ChessCollection>();
        
        WebApplication app = builder.Build();
        
        
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/", () => Results.Redirect($"/chess", permanent: false));
        // app.MapGet("/", () => Results.Redirect($"/number_adder", permanent: false));
        
        RouteGroupBuilder numberAdder = app.MapGroup("/number_adder");

        numberAdder.MapGet("/{id:int}", (int id) => {
                Console.WriteLine($"User has sent a number adder of int {id}");
                return TypedResults.Ok(1000 - id);
            }
        );

        
        RouteGroupBuilder messenger = app.MapGroup("/messenger");
        
        
        messenger.MapGet("/all", GetAllMessages);
        messenger.MapPost("/new-message", PostNewMessage);
        
        messenger.MapGet("/last-messages/{count:int}", (int count, ConcurrentMessengerCollection db) => {
            return TypedResults.Ok(db.GetLastNMessages(count).Select(m=>m.ToDto()));
        });
        
        messenger.MapGet("messages/{id:int}", (int id, ConcurrentMessengerCollection db) => {
            Message? message = db.GetMessage(id);
            return message is not null
                ? Results.Ok(message.ToDto())
                : Results.NotFound($"Message {id} not found");
        });
        
        RouteGroupBuilder chess = app.MapGroup("/chess");
        
        chess.MapGet("/new-game", RequestNewChessGame);
        chess.MapPost("/move", GetChessMove);
        
        app.Run();

    }

    static IResult RequestNewChessGame(ChessCollection games) {
        int newGameId = games.GetNewGame();
        return TypedResults.Ok(newGameId);
    }
    
    // static async Task<IResult> GetChessMove(ChessMoveDTO moveDto, ChessCollection games) {
    static async Task<IResult> GetChessMove(HttpContext ctx, ChessCollection games) {
        ctx.Request.EnableBuffering();
        
        // 1) Read raw text
        using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
        var rawJson = await reader.ReadToEndAsync();
        Console.WriteLine("RAW JSON: " + rawJson);
        
        // 2) Rewind
        ctx.Request.Body.Position = 0;
        
        // 3) Manually deserialize (or let the binder do it)
        var moveDto = await ctx.Request.ReadFromJsonAsync<ChessMoveDTO>(
            new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });

        ChessMoveDTO nextDTO;
        try {
            nextDTO = await games.GetNextMove(moveDto);
        }
        catch (Exception e) {
            Console.WriteLine(e);
            return TypedResults.NotFound($"Id {moveDto.GameId} has no currently played game.");
        }

        Console.WriteLine(moveDto.ToString());
        return TypedResults.Ok(nextDTO);
    }
    
    static  IResult PostNewMessage(MessageClientDTO messageServerDto, ConcurrentMessengerCollection db) {
        int id = db.AddNewMessage(messageServerDto);
        // Console.WriteLine(db.GetMessage(id));
        return TypedResults.Ok(id);
    }
    
    static IResult GetAllMessages(ConcurrentMessengerCollection db) {
        var messages = db.GetAllMessages();
        return TypedResults.Ok(messages.ToDto());
    }
    
}