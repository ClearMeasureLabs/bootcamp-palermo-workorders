using DbUp.Engine.Output;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Selects DbUp log implementations for CLI commands and tests.
/// </summary>
internal static class DatabaseUpgradeLogSelector
{
    internal static bool IsSilent(DatabaseOptions? options) =>
        options?.SuppressUpgradeConsoleOutput == true;

    internal static IUpgradeLog ForOptions(DatabaseOptions? options) =>
        IsSilent(options) ? new NullUpgradeLog() : new QuietLog();
}
