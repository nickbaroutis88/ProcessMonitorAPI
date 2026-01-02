namespace Host.ProcessMonitorAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Instantiate Startup and register services
        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        startup.Configure(app, app.Environment);

        app.Run();
    }
}
