using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClearMeasure.Bootcamp.IntegrationTests;

/// <summary>
/// Linux CI and local Linux runs often use SQLite while SQL Server runs in Docker or on Windows.
/// Tests marked <see cref="CategoryAttribute"/> <c>SqlServerOnly</c> must skip when the provider is not SQL Server.
/// </summary>
internal static class SqlServerTestAssumptions
{
    public static void RequireSqlServer()
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        Assume.That(
            context.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true,
            "Requires Microsoft SQL Server; skipped when integration tests use SQLite.");
    }
}
