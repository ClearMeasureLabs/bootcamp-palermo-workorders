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
        return _configuration.GetConnectionString("Sql") ?? throw new InvalidOperationException();
    }
}