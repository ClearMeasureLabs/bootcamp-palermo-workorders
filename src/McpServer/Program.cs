using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Resources;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var useHttp = args.Contains("--http") ||
    string.Equals(builder.Configuration["Transport"], "http", StringComparison.OrdinalIgnoreCase);

// Stdio transport owns stdout for MCP protocol frames; logs must go to stderr.
builder.AddServiceDefaults(logToStandardError: !useHttp);

if (!useHttp)
{
    // Serilog forwards to registered providers; the default console provider writes
    // to stdout, so it must also be redirected to stderr in stdio mode.
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
}

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

if (useHttp)
{
    mcpBuilder.WithHttpTransport();
}
else
{
    mcpBuilder.WithStdioServerTransport();
}

var app = builder.Build();

app.UseSerilogShutdown();

if (useHttp)
{
    app.UseCorrelationId();
    app.MapDefaultEndpoints();
    app.MapMcp();
}

await app.RunAsync();
