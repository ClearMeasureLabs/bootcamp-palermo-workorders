using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Populates <see cref="ApplicationFeatureFlags"/> from configuration once when the host starts.
/// </summary>
public sealed class ApplicationFeatureFlagsStartupInitializer(
    IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ApplicationFeatureFlags.HydrateFrom(featureFlagsOptions.Value);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
