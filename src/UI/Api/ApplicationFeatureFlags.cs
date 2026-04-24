using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static registry of application feature flag names. Runtime enabled/disabled values are taken from
/// <see cref="DiagnosticsFeatureFlagsOptions"/> (bound from the <c>FeatureFlags</c> configuration section) on each build.
/// </summary>
public static class ApplicationFeatureFlags
{
    private static readonly IReadOnlyDictionary<string, Func<DiagnosticsFeatureFlagsOptions, bool>> Registry =
        new Dictionary<string, Func<DiagnosticsFeatureFlagsOptions, bool>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(DiagnosticsFeatureFlagsOptions.SampleFeatureA)] = o => o.SampleFeatureA,
            [nameof(DiagnosticsFeatureFlagsOptions.SampleFeatureB)] = o => o.SampleFeatureB
        };

    /// <summary>
    /// Known flag names in stable order for API responses.
    /// </summary>
    public static IReadOnlyList<string> KnownNames { get; } = Registry.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Builds a response listing each registered flag with its current value from configuration.
    /// </summary>
    public static FeatureFlagsResponse BuildSnapshot(IOptions<DiagnosticsFeatureFlagsOptions> options)
    {
        var value = options.Value;
        var items = KnownNames.Select(name => new FeatureFlagItem(name, Registry[name](value))).ToList();
        return new FeatureFlagsResponse(items);
    }
}
