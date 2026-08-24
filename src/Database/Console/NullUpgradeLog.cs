using DbUp.Engine.Output;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// DbUp logger that suppresses all console output. Used when failure paths are exercised intentionally in tests.
/// </summary>
public sealed class NullUpgradeLog : IUpgradeLog
{
    public void LogTrace(string format, params object[] args)
    {
    }

    public void LogDebug(string format, params object[] args)
    {
    }

    public void LogInformation(string format, params object[] args)
    {
    }

    public void LogWarning(string format, params object[] args)
    {
    }

    public void LogError(string format, params object[] args)
    {
    }

    public void LogError(Exception ex, string format, params object[] args)
    {
    }
}
