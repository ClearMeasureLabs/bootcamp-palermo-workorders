using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class NeedsRebootHealthCheckTests
{
    private NeedsRebootHealthCheck _healthCheck = null!;

    [SetUp]
    public void SetUp()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
        _healthCheck = new NeedsRebootHealthCheck(NullLogger<NeedsRebootHealthCheck>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
    }

    [Test]
    public async Task CheckHealthAsync_WhenNeedsRebootFalse_ReturnsHealthy()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("NeedsReboot", _healthCheck, null, null)
        };
        var result = await _healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenNeedsRebootTrue_ReturnsUnhealthy()
    {
        NeedsRebootHealthCheck.NeedsReboot = true;

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("NeedsReboot", _healthCheck, null, null)
        };
        var result = await _healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("memory is corrupted. Restart process");
    }
}
