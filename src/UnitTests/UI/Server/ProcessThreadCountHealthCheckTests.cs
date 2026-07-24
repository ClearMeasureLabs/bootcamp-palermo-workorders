using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ProcessThreadCountHealthCheckTests
{
    private ProcessThreadCountHealthCheck _healthCheck = null!;

    [SetUp]
    public void SetUp()
    {
        _healthCheck = new ProcessThreadCountHealthCheck(NullLogger<ProcessThreadCountHealthCheck>.Instance);
    }

    [Test]
    public async Task CheckHealthAsync_Should_ReturnHealthyAndIncludeThreadCountInData()
    {
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("ProcessThreadCount", _healthCheck, null, null)
        };

        var result = await _healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ContainsKey("threadCount").ShouldBeTrue();
        result.Data["threadCount"].ShouldBe(Process.GetCurrentProcess().Threads.Count);
        ((int)result.Data["threadCount"]).ShouldBeGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task CheckHealthAsync_Should_ReportThreadCountAsInteger()
    {
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("ProcessThreadCount", _healthCheck, null, null)
        };

        var result = await _healthCheck.CheckHealthAsync(context);

        result.Data["threadCount"].ShouldBeOfType<int>();
    }
}
