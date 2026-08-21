using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using ClearMeasure.Bootcamp.UI.Server.Handlers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ServerHealthCheckHandlerTests
{
    [TestCase(HealthStatus.Healthy)]
    [TestCase(HealthStatus.Degraded)]
    [TestCase(HealthStatus.Unhealthy)]
    public async Task Handle_Should_ReturnStatusFromHealthCheckService(HealthStatus expected)
    {
        var handler = new ServerHealthCheckHandler(new StubHealthCheckService(expected));

        var result = await handler.Handle(new ServerHealthCheckQuery(), CancellationToken.None);

        result.ShouldBe(expected);
    }

    private sealed class StubHealthCheckService(HealthStatus status) : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["stub"] = new(status, description: null, TimeSpan.Zero, exception: null, data: null)
            };
            return Task.FromResult(new HealthReport(entries, TimeSpan.Zero));
        }
    }
}
