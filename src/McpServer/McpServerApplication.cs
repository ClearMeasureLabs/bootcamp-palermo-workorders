using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChurchBulletin.ServiceDefaults;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Host bootstrap for standalone MCP server startup.
/// </summary>
public static class McpServerApplication
{
    /// <summary>
    /// Determines whether the MCP server should use HTTP transport.
    /// </summary>
    public static bool ShouldUseHttpTransport(string[] args, IConfiguration configuration) =>
        args.Contains("--http") ||
        string.Equals(configuration["Transport"], "http", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Builds and runs the MCP server host.
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        var app = BuildApplication(args);
        await app.RunAsync();
    }

    internal static WebApplication BuildApplication(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddSerilogJsonConsole();

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

        configureBuilder?.Invoke(builder);

        var useHttp = ShouldUseHttpTransport(args, builder.Configuration);
        ConfigureTransport(mcpBuilder, useHttp);

        var app = builder.Build();

        app.UseSerilogShutdown();
        ConfigurePipeline(app, useHttp);

        return app;
    }

    internal static void ConfigureTransport(IMcpServerBuilder mcpBuilder, bool useHttp)
    {
        if (useHttp)
        {
            mcpBuilder.WithHttpTransport();
        }
        else
        {
            mcpBuilder.WithStdioServerTransport();
        }
    }

    internal static void ConfigurePipeline(WebApplication app, bool useHttp)
    {
        if (useHttp)
        {
            app.UseCorrelationId();
            app.MapMcp("/mcp");
        }
    }
}
