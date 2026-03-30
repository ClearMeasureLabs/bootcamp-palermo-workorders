using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Resources;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName);
});

builder.Host.UseLamar(registry => { registry.IncludeRegistry<McpServiceRegistry>(); });

var mcpBuilder = builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "ChurchBulletin",
            Version = "1.0.0"
        };
    })
    .WithTools<WorkOrderTools>()
    .WithTools<EmployeeTools>()
    .WithResources<ReferenceResources>();

var useHttp = args.Contains("--http") ||
    string.Equals(builder.Configuration["Transport"], "http", StringComparison.OrdinalIgnoreCase);

if (useHttp)
{
    mcpBuilder.WithHttpTransport();
}
else
{
    mcpBuilder.WithStdioServerTransport();
}

var app = builder.Build();

if (useHttp)
{
    app.MapMcp();
}

await app.RunAsync();
