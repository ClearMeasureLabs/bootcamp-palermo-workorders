using ClearMeasure.Bootcamp.Core.Model.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class TelemetryHandler(ILogger<TelemetryHandler> logger)
    : INotificationHandler<UserLoggedInEvent>
{
    private static readonly Meter Meter = new("ChurchBulletin.Application", "1.0.0");

    public static readonly Counter<long> LoginCounter = Meter.CreateCounter<long>(
        "app.user.logins",
        unit: "{logins}",
        description: "Number of user login events");

    public Task Handle(UserLoggedInEvent request, CancellationToken cancellationToken)
    {
        LoginCounter.Add(1, new KeyValuePair<string, object?>("user.name", request.UserName));
        logger.LogInformation("Recorded login metric for {UserName}", request.UserName);
        return Task.CompletedTask;
    }
}