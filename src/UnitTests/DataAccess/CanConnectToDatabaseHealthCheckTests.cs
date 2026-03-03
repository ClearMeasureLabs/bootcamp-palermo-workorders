using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class CanConnectToDatabaseHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        await using var dataContext = new DataContext(new StubDatabaseConfiguration("Data Source=:memory:"),
            NullLogger<DataContext>.Instance);
        var healthCheck = new CanConnectToDatabaseHealthCheck(dataContext, NullLogger<CanConnectToDatabaseHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("DataAccess", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenDatabaseIsUnreachable_ReturnsUnhealthy()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var missingFile = Path.Combine(missingDirectory, "missing.db");
        var connectionString = $"Data Source={missingFile};Mode=ReadOnly;";
        await using var dataContext = new DataContext(new StubDatabaseConfiguration(connectionString),
            NullLogger<DataContext>.Instance);
        var healthCheck = new CanConnectToDatabaseHealthCheck(dataContext, NullLogger<CanConnectToDatabaseHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("DataAccess", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    private class StubDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString()
        {
            return connectionString;
        }

        public void ResetConnectionPool()
        {
        }
    }
}
