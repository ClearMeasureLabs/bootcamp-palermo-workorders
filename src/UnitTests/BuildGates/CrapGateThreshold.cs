using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Reads the production CRAP gate threshold from the single authoritative config file.
/// </summary>
public static class CrapGateThreshold
{
    /// <summary>
    /// Relative path from the repository root to the gate threshold config.
    /// </summary>
    public const string RelativeConfigPath =
        ".cursor/skills/crap-score-cleanup/crap-gate-threshold.json";

    /// <summary>
    /// Reads <c>productionThreshold</c> from <see cref="RelativeConfigPath"/>.
    /// </summary>
    public static int ReadProductionThreshold()
    {
        var path = FindRepoFile(RelativeConfigPath);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("productionThreshold", out var property)
            || property.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException(
                $"{RelativeConfigPath} must contain a numeric productionThreshold property.");
        }

        return property.GetInt32();
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"{relativePath} not found from test directory.");
    }
}
