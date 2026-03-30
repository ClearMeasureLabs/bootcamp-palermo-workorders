namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Connection string for SQLite shared in-memory mode used by <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
/// hosts in the <c>Testing</c> environment so the app and tests can open the same database.
/// </summary>
public static class WebApplicationTestingDatabase
{
    public const string SqliteSharedMemoryConnectionString = "Data Source=ui-server-waf;Mode=Memory;Cache=Shared";
}
