using System.Diagnostics.Metrics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;

namespace ClearMeasure.Bootcamp.McpServer.Auditing;

/// <summary>Persists demo-session events without treating a reported identity as server authentication.</summary>
public sealed class AuthenticationAuditHandler(IAuditLog auditLog)
    : INotificationHandler<UserLoggedInEvent>, INotificationHandler<UserLoggedOutEvent>
{
    private static readonly Meter Meter = new("ChurchBulletin.Application", "1.0.0");
    private static readonly Counter<long> LogoutCounter = Meter.CreateCounter<long>(
        "app.user.logouts", "{logouts}", "Number of user logout events");

    /// <summary>Records the login event with the server-captured request metadata.</summary>
    public Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken) =>
        WriteAsync("Login", notification.UserName, cancellationToken);

    /// <summary>Records the logout event and increments the identity-free logout counter.</summary>
    public async Task Handle(UserLoggedOutEvent notification, CancellationToken cancellationToken)
    {
        await WriteAsync("Logout", notification.UserName, cancellationToken);
        LogoutCounter.Add(1);
    }

    private async Task WriteAsync(string action, string? reportedUserName, CancellationToken cancellationToken)
    {
        var metadata = AuditContext.Current;
        var actor = metadata.ActorUserName;
        if ((string.Equals(actor, "system", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(actor, "anonymous", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(reportedUserName))
            actor = $"demo:{reportedUserName}";

        using var scope = AuditContext.Begin(metadata with { ActorUserName = actor, Action = action });
        await auditLog.WriteAsync(action, "Authentication", cancellationToken: cancellationToken);
    }
}
