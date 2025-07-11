using App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

internal class Program {
    public static void Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        // builder.Services.AddDbContext<MessengerDb>(opt => opt.UseInMemoryDatabase("Messenger"));
        // builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddSingleton<ConcurrentMessengerCollection>();
        WebApplication app = builder.Build();
        
        
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/", () => Results.Redirect($"/messenger", permanent: false));
        
        RouteGroupBuilder numberAdder = app.MapGroup("/number_adder");
        
        numberAdder.MapGet("/{id:int}", (int id) => 
            TypedResults.Ok(1000 - id));

        RouteGroupBuilder messenger = app.MapGroup("/messenger");
        
        messenger.MapGet("/all", GetAllMessages);
        messenger.MapPost("/{id:int}", GetMessage);
        messenger.MapPost("/", CreateMessage);
        
        app.Run();

    }

    static StaticFileOptions GetFile(string pathFromContentRoot, WebApplicationBuilder builder) {
        return new StaticFileOptions {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "number_adder")
            ),
            RequestPath = ""
        };
    }
    
    static async Task<IResult> GetMessage(int id, ConcurrentMessengerCollection db) {
        await db.AsyncNothing();
        try {
            var message = db.GetMessage(id);
            return TypedResults.Ok(message);
        }
        catch (Exception) {
            return TypedResults.NotFound();
        }
    }
    
    static async Task<IResult> GetAllMessages(ConcurrentMessengerCollection db) {
        var messages = db.GetAllMessages();
        await db.AsyncNothing();
        return TypedResults.Ok(messages);
    }

    static async Task<IResult> CreateMessage(Message message, ConcurrentMessengerCollection db) {
        
        db.AddMessage(message);
            
        await db.AsyncNothing();

        return TypedResults.Created($"/messages/{message.Id}", message);
    }
    
}