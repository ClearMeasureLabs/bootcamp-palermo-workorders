using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class DatabaseTests
{
    [Test]
    [Explicit]
    [Category("DataSchema")]
    public void CreateDatabaseSchema()
    {
        var context = TestHost.GetRequiredService<DbContext>();
        context.Database.EnsureCreated();
    }

    public void Clean()
    {
        new DatabaseEmptier(TestHost.GetRequiredService<DbContext>().Database).DeleteAllData();
    }
}