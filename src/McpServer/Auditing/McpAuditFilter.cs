using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Auditing;

/// <summary>Audits every MCP tool invocation without recording arguments, results or exception details.</summary>
public static class McpAuditFilter
{
    /// <summary>Wraps the SDK tool dispatcher, including read tools and rejected invocations.</summary>
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Wrap(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancellationToken) =>
        {
            var services = context.Services ?? throw new InvalidOperationException("MCP request services are required.");
            var http = services.GetService<IHttpContextAccessor>()?.HttpContext;
            var metadata = http is not null ? AuditRequestMiddleware.CreateContext(http)
                : new AuditContext(ActorUserName: context.User?.Identity is { IsAuthenticated: true } identity
                    ? identity.Name ?? "anonymous" : "anonymous");
            using var scope = AuditContext.Begin(metadata with { Action = "McpToolInvoked" });
            var toolName = context.MatchedPrimitive is McpServerTool tool ? tool.ProtocolTool.Name
                : context.Params?.Name is { } name
                  && context.Server.ServerOptions.ToolCollection?.TryGetPrimitive(name, out var registered) == true
                    ? registered.ProtocolTool.Name : "unknown";
            return await InvokeAsync(services.GetRequiredService<IServiceScopeFactory>(),
                services.GetRequiredService<ILoggerFactory>().CreateLogger("McpAudit"), toolName,
                () => next(context, cancellationToken));
        };

    /// <summary>Records the SDK dispatch outcome, not domain success, and preserves original exceptions.</summary>
    public static async ValueTask<CallToolResult> InvokeAsync(
        IServiceScopeFactory scopeFactory, ILogger logger, string? toolName, Func<ValueTask<CallToolResult>> invoke)
    {
        CallToolResult result;
        try
        {
            result = await invoke();
        }
        catch
        {
            try
            {
                await WriteOutcomeAsync(scopeFactory, toolName, "exception");
            }
            catch (Exception auditException)
            {
                logger.LogError(auditException, "MCP invocation audit persistence failed");
            }
            throw;
        }

        await WriteOutcomeAsync(scopeFactory, toolName, result.IsError == true ? "error" : "returned");
        return result;
    }

    private static async Task WriteOutcomeAsync(IServiceScopeFactory scopeFactory, string? toolName, string outcome)
    {
        // The request may already be cancelled; allow a bounded independent attempt to persist its outcome.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var scope = scopeFactory.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLog>();
        await audit.WriteAsync("McpToolInvoked", "McpTool", newValue: JsonSerializer.Serialize(new
        {
            tool = toolName is null ? "unknown" : toolName[..Math.Min(toolName.Length, 128)],
            outcome
        }), cancellationToken: timeout.Token);
    }
}
