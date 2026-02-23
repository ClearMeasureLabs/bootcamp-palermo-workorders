using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Server;

public class DatabaseConfiguration(IConfiguration configuration) : IDatabaseConfiguration
{
    public string GetConnectionString()
    {
        return configuration.GetConnectionString("SqlConnectionString") ??
               throw new InvalidOperationException("SqlConnectionString is missing");
    }

    public string GetDatabaseProvider()
    {
        return configuration["DatabaseProvider"] ?? "SqlServer";
    }
}