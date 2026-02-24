using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.McpServer;

public class DatabaseConfiguration(IConfiguration configuration) : IDatabaseConfiguration
{
    public string GetConnectionString()
    {
        return configuration.GetConnectionString("SqlConnectionString") ??
               throw new InvalidOperationException("SqlConnectionString is missing");
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
