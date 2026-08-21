using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class HealthCheckRemotableRequestHandlerTests
{
    [TestCase(HealthStatus.Healthy)]
    [TestCase(HealthStatus.Degraded)]
    [TestCase(HealthStatus.Unhealthy)]
    public async Task Handle_ShouldEchoRequestStatus(HealthStatus status)
    {
        var handler = new HealthCheckRemotableRequestHandler();

        var result = await handler.Handle(new HealthCheckRemotableRequest(status), CancellationToken.None);

        result.ShouldBe(status);
    }

    [Test]
    public async Task Handle_ShouldBeResolvableFromTestHost()
    {
        var handler = TestHost.GetRequiredService<HealthCheckRemotableRequestHandler>();

        var result = await handler.Handle(
            new HealthCheckRemotableRequest(HealthStatus.Degraded),
            CancellationToken.None);

        result.ShouldBe(HealthStatus.Degraded);
    }
}
