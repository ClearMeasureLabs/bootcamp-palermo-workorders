using ClearMeasure.Bootcamp.Core.Model.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Records a metric when dated work orders are created in a batch.
/// </summary>
public class DatedWorkOrdersCreatedTelemetryHandler(ILogger<DatedWorkOrdersCreatedTelemetryHandler> logger)
    : INotificationHandler<DatedWorkOrdersCreatedEvent>
{
    private static readonly Meter Meter = new("ChurchBulletin.Application", "1.0.0");

    private static readonly Counter<long> CreatedCounter = Meter.CreateCounter<long>(
        "app.workorders.dated_batch_created",
        unit: "{workorders}",
        description: "Number of work orders created via dated batch create");

    public Task Handle(DatedWorkOrdersCreatedEvent notification, CancellationToken cancellationToken)
    {
        CreatedCounter.Add(notification.Count);
        logger.LogInformation("Recorded dated batch create metric for {Count} work orders", notification.Count);
        return Task.CompletedTask;
    }
}
