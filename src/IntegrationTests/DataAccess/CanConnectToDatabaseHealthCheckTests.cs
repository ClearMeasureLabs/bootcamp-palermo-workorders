using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class CanConnectToDatabaseHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenDatabaseReachable_ReturnsHealthyWithProviderName()
    {
        var dbContext = TestHost.GetRequiredService<DbContext>();
        var sut = new CanConnectToDatabaseHealthCheck(dbContext,
            NullLogger<CanConnectToDatabaseHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain(dbContext.Database.ProviderName!);
    }

    [Test]
    public async Task CheckHealthAsync_WhenDbContextCannotConnect_ReturnsUnhealthyWithProviderName()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
        var connectionString = $"Data Source={Path.Combine(missingDir, "test.db")}";
        await using var dbContext = new DataContext(new StubMissingDirectoryDatabaseConfiguration(connectionString));
        var sut = new CanConnectToDatabaseHealthCheck(dbContext,
            NullLogger<CanConnectToDatabaseHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        (result.Description.StartsWith("Database connection failed", StringComparison.Ordinal)
            || result.Description.StartsWith("Cannot connect", StringComparison.Ordinal)).ShouldBeTrue();
        result.Description.ShouldContain("Provider:");
    }

    private sealed class StubMissingDirectoryDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
