using System.Xml.Linq;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Detects whether Cobertura XML includes production <c>ClearMeasure.Bootcamp.Core</c>
/// with at least one line hit.
/// </summary>
public static class CoreCoberturaPresence
{
    /// <summary>
    /// Production Core Cobertura package name emitted by Coverlet.
    /// </summary>
    public const string CorePackageName = "ClearMeasure.Bootcamp.Core";

    /// <summary>
    /// Returns true when the Core package (or production Core filenames) has a line with hits &gt; 0.
    /// </summary>
    public static bool HasProductionCoreHits(string coberturaXml)
    {
        if (string.IsNullOrWhiteSpace(coberturaXml))
        {
            return false;
        }

        var document = XDocument.Parse(coberturaXml);

        foreach (var package in document.Descendants("package"))
        {
            var packageName = package.Attribute("name")?.Value;
            if (!string.Equals(packageName, CorePackageName, StringComparison.Ordinal))
            {
                continue;
            }

            if (HasAnyLineHit(package))
            {
                return true;
            }
        }

        foreach (var classElement in document.Descendants("class"))
        {
            var filename = classElement.Attribute("filename")?.Value;
            var className = classElement.Attribute("name")?.Value;
            if (!IsProductionCoreClass(filename, className))
            {
                continue;
            }

            if (HasAnyLineHit(classElement))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True for production Core paths (<c>src/Core/...</c> or Coverlet <c>Core\...</c>),
    /// false for UnitTests.Core or other trees.
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
               || lower.StartsWith("src/core/", StringComparison.Ordinal)
               || lower.StartsWith("core/", StringComparison.Ordinal);
    }

    /// <summary>
    /// True when the Cobertura class belongs to production Core (package path or type name).
    /// </summary>
    public static bool IsProductionCoreClass(string? filename, string? className)
    {
        if (IsProductionCoreFilename(filename))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(className))
        {
            return false;
        }

        return className.StartsWith(CorePackageName + ".", StringComparison.Ordinal)
               && !className.StartsWith("ClearMeasure.Bootcamp.UnitTests.", StringComparison.Ordinal);
    }

    private static bool HasAnyLineHit(XElement scope)
    {
        foreach (var line in scope.Descendants("line"))
        {
            if (int.TryParse(line.Attribute("hits")?.Value, out var hits) && hits > 0)
            {
                return true;
            }
        }

        return false;
    }
}
