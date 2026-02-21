using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[SetUpFixture]
public class SqliteDatabaseSetup
{
    [OneTimeSetUp]
    public void SetUp()
    {
        var context = TestHost.GetRequiredService<DbContext>();
        if (context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Database.EnsureCreated();
        }
    }
}
