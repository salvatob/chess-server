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

        builder.Services.AddSingleton<ChessManager>();

        WebApplication app = builder.Build();

        app.UseWebSockets();

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

        RouteGroupBuilder chess = app.MapGroup("/chess");

       
        app.Run();

    }
}