using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Returns a fixed detailed health report so conditional GET / ETag tests are deterministic.
/// </summary>
internal sealed class StubFixedDetailedHealthReportProvider : IDetailedHealthReportProvider
{
    private static readonly DetailedHealthReport FixedReport = new()
    {
        OverallStatus = ComponentHealthStatus.Healthy,
        CheckedAtUtc = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
        Components =
        [
            new ComponentHealthEntry
            {
                Name = "API",
                Status = ComponentHealthStatus.Healthy,
                DurationMs = 1.0
            }
        ]
    };

    public Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(FixedReport);
}
