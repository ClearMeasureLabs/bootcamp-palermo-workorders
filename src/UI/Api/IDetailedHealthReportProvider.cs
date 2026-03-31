namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Produces a <see cref="DetailedHealthReport"/> by aggregating registered health checks (excluding liveness-only probes).
/// </summary>
public interface IDetailedHealthReportProvider
{
    /// <summary>
    /// Runs all non-live health checks and returns a JSON-oriented report.
    /// </summary>
    Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default);
}
