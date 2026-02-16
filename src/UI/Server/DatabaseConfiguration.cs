using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Server;

public class DatabaseConfiguration(IConfiguration configuration) : IDatabaseConfiguration
{
    public string GetConnectionString()
    {
        return configuration.GetConnectionString("Sql") ??
               throw new InvalidOperationException("ConnectionStrings:Sql is missing");
    }
}