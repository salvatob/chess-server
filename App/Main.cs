using App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

internal class Program {
    public static void Main(string[] args) {

        
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions());
        // builder.WebHost.UseUrls("http://0.0.0.0:5000");
        
        builder.Services.AddSingleton<ConcurrentMessengerCollection>();
        
        WebApplication app = builder.Build();
        
        
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/", () => Results.Redirect($"/number_adder", permanent: false));
        
        RouteGroupBuilder numberAdder = app.MapGroup("/number_adder");

        numberAdder.MapGet("/{id:int}", (int id) => {
                Console.WriteLine($"User has sent a number adder of int {id}");
                return TypedResults.Ok(1000 - id);
            }
        );

        RouteGroupBuilder messenger = app.MapGroup("/messenger");
        
        
        messenger.MapGet("/all", GetAllMessages);
        messenger.MapPost("/new-message", PostNewMessage);
        
        app.Run();

    }
    
    
    static IResult PostNewMessage(MessageDTO messageDto, ConcurrentMessengerCollection db) {
        int id = db.AddNewMessage(messageDto);
        return TypedResults.Ok(id);
    }
    
    static async Task<IResult> GetAllMessages(ConcurrentMessengerCollection db) {
        var messages = db.GetAllMessages();
        await db.AsyncNothing();
        return TypedResults.Ok(messages);
    }

    // static async Task<IResult> CreateMessage(Message message, ConcurrentMessengerCollection db) {
    //     
    //     db.AddMessage(message);
    //         
    //     await db.AsyncNothing();
    //
    //     return TypedResults.Created($"/messages/{message.Id}", message);
    // }
    
}