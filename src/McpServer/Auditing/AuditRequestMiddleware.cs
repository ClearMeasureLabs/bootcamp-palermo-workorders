using ClearMeasure.Bootcamp.Core;
using ChurchBulletin.ServiceDefaults;

namespace ClearMeasure.Bootcamp.McpServer.Auditing;

/// <summary>Captures server-observed request metadata without trusting client identity headers.</summary>
public sealed class AuditRequestMiddleware(RequestDelegate next)
{
    /// <summary>Applies bounded audit metadata for the lifetime of the request.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = AuditContext.Begin(CreateContext(context));
        await next(context);
    }

    /// <summary>Creates metadata exclusively from the authenticated principal and connection.</summary>
    public static AuditContext CreateContext(HttpContext context) => new(
        ActorUserName: Bound(context.User.Identity is { IsAuthenticated: true } identity
            ? identity.Name ?? "anonymous" : "anonymous", 255)!,
        IpAddress: Bound(context.Connection.RemoteIpAddress?.ToString(), 45),
        UserAgent: Bound(context.Request.Headers.UserAgent.ToString(), 500),
        CorrelationId: Bound(context.Items[CorrelationIdConstants.HttpContextItemKey] as string
            ?? context.TraceIdentifier, 64));

    private static string? Bound(string? value, int maximum) =>
        value is null ? null : value[..Math.Min(value.Length, maximum)];
}
