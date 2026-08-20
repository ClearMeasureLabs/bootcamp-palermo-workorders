using System.Xml.Linq;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Detects whether Cobertura XML includes production <c>ClearMeasure.Bootcamp.Core</c>
/// source under <c>src/Core/</c> with at least one line hit.
/// </summary>
public static class CoreCoberturaPresence
{
    /// <summary>
    /// Returns true when any class filename is production Core and has a line with hits &gt; 0.
    /// </summary>
    public static bool HasProductionCoreHits(string coberturaXml)
    {
        if (string.IsNullOrWhiteSpace(coberturaXml))
        {
            return false;
        }

        var document = XDocument.Parse(coberturaXml);
        foreach (var classElement in document.Descendants("class"))
        {
            var filename = classElement.Attribute("filename")?.Value;
            if (!IsProductionCoreFilename(filename))
            {
                continue;
            }

            foreach (var line in classElement.Descendants("line"))
            {
                if (int.TryParse(line.Attribute("hits")?.Value, out var hits) && hits > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True for production Core paths (<c>src/Core/...</c>), false for UnitTests.Core or other trees.
    /// </summary>
    public static bool IsProductionCoreFilename(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return false;
        }

        var normalized = filename.Replace('\\', '/');
        var lower = normalized.ToLowerInvariant();
        if (lower.Contains("/unittests/", StringComparison.Ordinal)
            || lower.Contains("/integrationtests/", StringComparison.Ordinal)
            || lower.Contains("/acceptancetests/", StringComparison.Ordinal))
        {
            return false;
        }

        return lower.Contains("/src/core/", StringComparison.Ordinal)
               || lower.StartsWith("src/core/", StringComparison.Ordinal);
    }
}
