namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Provides a consistent Factory Run ID (GUID) generated once at application startup
/// and used to correlate health check requests across the deployment pipeline.
/// </summary>
public interface IFactoryRunIdProvider
{
    /// <summary>
    /// Gets the Factory Run ID generated at application startup.
    /// </summary>
    string RunId { get; }
}

/// <summary>
/// Default implementation of <see cref="IFactoryRunIdProvider"/> that generates
/// a GUID at application startup.
/// </summary>
public sealed class FactoryRunIdProvider : IFactoryRunIdProvider
{
    /// <summary>
    /// Gets the Factory Run ID generated at application startup.
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FactoryRunIdProvider"/> class
    /// with a randomly generated GUID.
    /// </summary>
    public FactoryRunIdProvider()
    {
        RunId = Guid.NewGuid().ToString();
    }
}
