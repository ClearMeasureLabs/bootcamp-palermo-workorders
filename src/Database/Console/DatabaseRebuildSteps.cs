using DbUp;
using DbUp.Engine.Output;
using DbUp.Engine;
using DbUp.Helpers;
using DbUp.Support;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Runs DbUp upgrade steps for a full database rebuild.
/// </summary>
public static class DatabaseRebuildSteps
{
    /// <summary>
    /// Runs Create, Update, Everytime, then TestData for a full rebuild.
    /// </summary>
    public static DatabaseResult RunFullRebuild(string connectionString, string scriptDir, IUpgradeLog? upgradeLog = null)
    {
        var log = upgradeLog ?? new QuietLog();
        var createUpdate = RunCreateAndUpdate(connectionString, scriptDir, log);
        if (!createUpdate.Successful)
        {
            return createUpdate;
        }

        var everytime = RunEverytime(connectionString, scriptDir, log);
        if (!everytime.Successful)
        {
            return everytime;
        }

        return RunTestData(connectionString, scriptDir, log);
    }

    /// <summary>
    /// Runs Create and Update scripts (journaled, run-once).
    /// </summary>
    private static DatabaseResult RunCreateAndUpdate(string connectionString, string scriptDir, IUpgradeLog log)
    {
        var engine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Create"))
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Update"))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogTo(log)
            .Build();

        return ToResult(engine.PerformUpgrade(), "Could not run scripts to rebuild database.");
    }

    /// <summary>
    /// Runs Everytime scripts (run-always, not journaled).
    /// </summary>
    private static DatabaseResult RunEverytime(string connectionString, string scriptDir, IUpgradeLog log)
    {
        var engine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(
                Path.Join(scriptDir, "Everytime"),
                new SqlScriptOptions { ScriptType = ScriptType.RunAlways })
            .JournalTo(new NullJournal())
            .LogTo(log)
            .Build();

        return ToResult(engine.PerformUpgrade(), "Failed to re-apply RunAlways scripts.");
    }

    /// <summary>
    /// Runs TestData scripts (journaled).
    /// </summary>
    private static DatabaseResult RunTestData(string connectionString, string scriptDir, IUpgradeLog log)
    {
        var engine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "TestData"))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogTo(log)
            .Build();

        return ToResult(engine.PerformUpgrade(), "Failed to run TestData scripts.");
    }

    private static DatabaseResult ToResult(DatabaseUpgradeResult result, string fallbackMessage)
    {
        return result.Successful
            ? DatabaseResult.Success()
            : DatabaseResult.Failure(result.Error?.ToString() ?? fallbackMessage);
    }
}

/// <summary>
/// Outcome of a database upgrade step.
/// </summary>
public readonly record struct DatabaseResult(bool Successful, string? ErrorMessage)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DatabaseResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DatabaseResult Failure(string message) => new(false, message);
}
