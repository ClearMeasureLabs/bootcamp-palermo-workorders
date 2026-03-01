using DbUp.Engine.Output;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// A quiet DbUp logger that suppresses informational messages.
/// Only warnings and errors are written to the console.
/// </summary>
public class QuietLog : IUpgradeLog
{
    public void LogTrace(string format, params object[] args)
    {
        // Suppressed
    }

    public void LogDebug(string format, params object[] args)
    {
        // Suppressed
    }

    public void LogInformation(string format, params object[] args)
    {
        // Suppressed to reduce build noise
    }

    public void LogWarning(string format, params object[] args)
    {
        System.Console.WriteLine("[WARNING] " + string.Format(format, args));
    }

    public void LogError(string format, params object[] args)
    {
        System.Console.Error.WriteLine("[ERROR] " + string.Format(format, args));
    }

    public void LogError(Exception ex, string format, params object[] args)
    {
        System.Console.Error.WriteLine("[ERROR] " + string.Format(format, args));
        System.Console.Error.WriteLine(ex.ToString());
    }
}
