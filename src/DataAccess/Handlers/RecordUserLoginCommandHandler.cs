using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Handles <see cref="RecordUserLoginCommand"/> by recording a login metric.
/// </summary>
public class RecordUserLoginCommandHandler(ILogger<RecordUserLoginCommandHandler> logger)
    : IRequestHandler<RecordUserLoginCommand, Unit>
{
    private static readonly Meter Meter = new("ChurchBulletin.Application", "1.0.0");

    public static readonly Counter<long> LoginCounter = Meter.CreateCounter<long>(
        "app.user.logins",
        unit: "{logins}",
        description: "Number of user login events");

    public Task<Unit> Handle(RecordUserLoginCommand request, CancellationToken cancellationToken)
    {
        LoginCounter.Add(1, new KeyValuePair<string, object?>("user.name", request.UserName));
        logger.LogInformation("Recorded login metric for {UserName}", request.UserName);
        return Task.FromResult(Unit.Value);
    }
}
