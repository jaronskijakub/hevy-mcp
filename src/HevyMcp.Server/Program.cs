using HevyMcp.Server.Hevy.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Configuration.AddUserSecrets<Program>();

var apiKey = builder.Configuration["Hevy:ApiKey"]
             ?? throw new InvalidOperationException(
                 "Missing Hevy:ApiKey. Run: dotnet user-secrets set \"Hevy:ApiKey\" <your-key>");

builder.Services.AddHttpClient<HevyClient>(client =>
{
    client.BaseAddress = new Uri("https://api.hevyapp.com/v1/");
    client.DefaultRequestHeaders.Add("api-key", apiKey);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Services.AddSingleton<ExerciseCatalog>();

await builder.Build().RunAsync();