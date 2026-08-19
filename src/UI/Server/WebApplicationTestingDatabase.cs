namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Connection string for SQLite shared in-memory mode used by <c>Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory&lt;TEntryPoint&gt;</c>
/// hosts in the <c>Testing</c> environment so the app and tests can open the same database.
/// </summary>
public static class WebApplicationTestingDatabase
{
    public const string SqliteSharedMemoryConnectionString = "Data Source=ui-server-waf;Mode=Memory;Cache=Shared";
}
