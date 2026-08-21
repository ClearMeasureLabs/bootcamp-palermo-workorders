namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Production-file filter for the CRAP production gate
/// (<c>crap-gate-threshold.json</c>). Keep in sync with
/// <c>IsProductionFile</c> in <c>.cursor/skills/crap-score-cleanup/scripts/rollup-file-scores.csx</c>.
/// </summary>
public static class CrapProductionScope
{
    /// <summary>
    /// Returns whether <paramref name="path"/> is in-scope production source
    /// (under <c>src/</c>, excluding test projects and generated code).
    /// </summary>
    public static bool IsProductionFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        if (IsExcludedTestPath(normalized) || IsGeneratedPath(normalized))
        {
            return false;
        }

        return IsUnderSrc(normalized);
    }

    private static bool IsExcludedTestPath(string normalized)
    {
        return normalized.Contains("/unittests/", StringComparison.Ordinal)
               || normalized.Contains("/integrationtests/", StringComparison.Ordinal)
               || normalized.Contains("/acceptancetests/", StringComparison.Ordinal);
    }

    private static bool IsGeneratedPath(string normalized)
    {
        return normalized.Contains("/generated/", StringComparison.Ordinal)
               || normalized.EndsWith(".g.cs", StringComparison.Ordinal)
               || normalized.EndsWith(".designer.cs", StringComparison.Ordinal);
    }

    private static bool IsUnderSrc(string normalized)
    {
        return normalized.Contains("/src/", StringComparison.Ordinal);
    }
}
