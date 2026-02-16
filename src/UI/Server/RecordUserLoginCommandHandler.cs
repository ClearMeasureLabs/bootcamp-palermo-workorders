using System.Diagnostics.Metrics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Commands;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Handles login metric recording on the server where OTEL exporters are active.
/// </summary>
public class RecordUserLoginCommandHandler : IRequestHandler<RecordUserLoginCommand, Unit>
{
    private static readonly Meter LoginMeter = new(TelemetryConstants.ApplicationSourceName);

    private static readonly Counter<long> LoginCounter = LoginMeter.CreateCounter<long>(
        "app.user.logins",
        description: "Number of user logins");

    public Task<Unit> Handle(RecordUserLoginCommand request, CancellationToken cancellationToken)
    {
        LoginCounter.Add(1,
            new KeyValuePair<string, object?>("user.name", request.UserName),
            new KeyValuePair<string, object?>("user.full_name", request.FullName));

        return Task.FromResult(Unit.Value);
    }
}