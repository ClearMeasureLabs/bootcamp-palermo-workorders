namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Configuration-bound feature flags exposed on <c>GET /api/diagnostics</c> for deployment verification.
/// </summary>
public sealed class DiagnosticsFeatureFlagsOptions
{
    /// <summary>Configuration section name (root <c>FeatureFlags</c> in appsettings).</summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>Sample flag for contract stability; replace or extend with product flags as needed.</summary>
    public bool SampleFeatureA { get; set; }

    /// <summary>Sample flag for contract stability; replace or extend with product flags as needed.</summary>
    public bool SampleFeatureB { get; set; }
}
