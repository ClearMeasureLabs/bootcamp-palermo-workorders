using DbUp.Engine.Output;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Selects DbUp log implementations for CLI commands and tests.
/// </summary>
internal static class DatabaseUpgradeLogSelector
{
    internal static IUpgradeLog ForOptions(DatabaseOptions? options) =>
        options?.SuppressUpgradeConsoleOutput == true ? new NullUpgradeLog() : new QuietLog();
}
