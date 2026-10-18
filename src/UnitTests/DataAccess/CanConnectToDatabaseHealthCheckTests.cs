using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class CanConnectToDatabaseHealthCheckTests
{
    private string _dbPath = null!;
    private DataContext _dbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        _dbContext = new DataContext(new StubDatabaseConfiguration($"Data Source={_dbPath}"));
        _dbContext.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task CheckHealthAsync_WhenCanConnect_ShouldReturnHealthyAndIncludeProviderInData()
    {
        var healthCheck = new CanConnectToDatabaseHealthCheck(_dbContext,
            NullLogger<CanConnectToDatabaseHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("DataAccess", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ContainsKey("provider").ShouldBeTrue();
        result.Data["provider"].ShouldBe(_dbContext.Database.ProviderName);
    }

    private sealed class StubDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
