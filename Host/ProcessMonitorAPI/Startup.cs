using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProcessMonitorApi.Mappers.Implementations;
using ProcessMonitorApi.Mappers.Interfaces;
using ProcessMonitorApi.Operations.Implementations;
using ProcessMonitorApi.Operations.Interfaces;
using ProcessMonitorApi.Repository;
using ProcessMonitorApi.Services.Implementations;
using ProcessMonitorApi.Services.Interfaces;
using Prometheus;
using System.Net.Http.Headers;

namespace Host.ProcessMonitorAPI;

internal class Startup
{
    private const string Latest = "latest";
    private const string ApiVersion = "1.0.0";

    private readonly IConfiguration configuration;

    public Startup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the controller services
        services.AddControllers();

        services.AddMvcCore();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
           options.SwaggerDoc(Latest, new OpenApiInfo { Title = $"Process Monitor API", Version = ApiVersion });
        });

        services.AddHealthChecks();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("SqliteConnection")));

        services.AddHttpClient<IHuggingFaceClassificationService, HuggingFaceClassificationService>(client =>
        {
            client.BaseAddress = new Uri(configuration.GetValue<string>("HuggingFaceSettings:Endpoint") ?? "https://router.huggingface.co/");
            client.Timeout = TimeSpan.FromSeconds(60); // Longer for large LLM responses
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.GetValue<string>("HuggingFaceSettings:TokenValue"));
        })
        .AddStandardResilienceHandler(); // Adds automatic retries and circuit breakers;;

        services.AddScoped<IAnalyzeOperation, AnalyzeOperation>();
        services.AddScoped<ISQLiteRepository, SQLiteRepository>();
        services.AddScoped<IAnalysisMapper, AnalysisMapper>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Initialize database
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
        }

        if (env.IsDevelopment())
        {
            // Optional if you want to enable request diagnostic middleware to log the full request in failure scenarios.
            // Note that this will log bearer tokens etc. so should only be used in non-PRD scenarios.
            //app.UseMiddleware<RequestDiagnosticMiddleware>();

            app.UseDeveloperExceptionPage();
        }

        app
            .UseSwagger()
            .UseSwaggerUI(options =>
                {
                    // Ensure this path matches where Swashbuckle is actually serving the JSON
                    options.SwaggerEndpoint($"{Latest}/swagger.json", ApiVersion);
                });

        app.UseRouting();

        app.UseHttpMetrics(options =>
        {
            options.InProgress.Enabled = false;
            options.RequestCount.Enabled = false;
            options.RequestDuration.Enabled = false;
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            // So we can potentially add liveness/readiness checks to our dependencies later

            // Liveness probe: returns 200 if the app is alive
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("liveness")
            });

            // Readiness probe: returns 200 ONLY if all readiness tags pass
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("readiness")
            });

            // Example Minimal API alongside controllers
            endpoints
                .MapMetrics(pattern: "/prometheus/metrics");
        });
    }
}
