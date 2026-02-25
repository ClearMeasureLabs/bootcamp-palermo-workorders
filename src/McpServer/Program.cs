using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Resources;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var useHttpTransport = args.Contains("--transport") &&
    args.SkipWhile(a => a != "--transport").Skip(1).FirstOrDefault() == "http";

if (useHttpTransport)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Host.UseLamar(registry => { registry.IncludeRegistry<McpServiceRegistry>(); });

    builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "ChurchBulletin",
                Version = "1.0.0"
            };
        })
        .WithHttpTransport()
        .WithTools<WorkOrderTools>()
        .WithTools<EmployeeTools>()
        .WithResources<ReferenceResources>();

    var app = builder.Build();
    app.MapMcp();
    await app.RunAsync();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.UseLamar(registry => { registry.IncludeRegistry<McpServiceRegistry>(); });

    builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "ChurchBulletin",
                Version = "1.0.0"
            };
        })
        .WithStdioServerTransport()
        .WithTools<WorkOrderTools>()
        .WithTools<EmployeeTools>()
        .WithResources<ReferenceResources>();

    await builder.Build().RunAsync();
}
