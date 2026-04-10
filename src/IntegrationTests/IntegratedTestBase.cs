using ClearMeasure.Bootcamp.UnitTests;
using NUnit.Framework;

namespace ClearMeasure.Bootcamp.IntegrationTests;

public class IntegratedTestBase
{
    /// <summary>
    /// Skips when <c>DATABASE_ENGINE=SQLite</c>. Use with <c>[Category("SqlServerOnly")]</c> tests that SQLite CI still discovers.
    /// </summary>
    protected static void SkipWhenSqliteEngine()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("DATABASE_ENGINE"), "SQLite",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Test requires SQL Server; skipped when DATABASE_ENGINE is SQLite.");
        }
    }

    protected TK Faker<TK>()
    {
        return TestHost.Faker<TK>();
    }

    public static void AssertAllProperties(object expected, object actual)
    {
        ObjectMother.AssertAllProperties(expected, actual);
    }
}