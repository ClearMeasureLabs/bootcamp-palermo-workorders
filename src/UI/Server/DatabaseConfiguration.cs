using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Server;

public class DatabaseConfiguration : IDatabaseConfiguration
{
    private readonly IConfiguration _configuration;

    public DatabaseConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("SqlConnectionString") ?? throw new InvalidOperationException("SqlConnectionString is missing");
    }
}