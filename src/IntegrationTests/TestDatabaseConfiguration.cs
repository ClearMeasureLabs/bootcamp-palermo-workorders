using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests;

public class TestDatabaseConfiguration : IDatabaseConfiguration
{
    private readonly IConfiguration _configuration;

    public TestDatabaseConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("SqlConnectionString") ?? throw new InvalidOperationException();
    }

    public void ResetConnectionPool()
    {
        var connectionString = GetConnectionString();
        if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        }
        else
        {
            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
        }
    }
}