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
        return HasHitsInCorePackage(document) || HasHitsInProductionCoreClasses(document);
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

        var lower = filename.Replace('\\', '/').ToLowerInvariant();
        if (IsExcludedTestFilename(lower))
        {
            return false;
        }

        return IsCorePath(lower);
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

    private static bool HasHitsInCorePackage(XDocument document)
    {
        foreach (var package in document.Descendants("package"))
        {
            if (PackageElementHasCoreHits(package))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PackageElementHasCoreHits(XElement package)
    {
        var packageName = GetAttributeValue(package, "name");
        if (!string.Equals(packageName, CorePackageName, StringComparison.Ordinal))
        {
            return false;
        }

        return HasAnyLineHit(package);
    }

    private static bool HasHitsInProductionCoreClasses(XDocument document)
    {
        foreach (var classElement in document.Descendants("class"))
        {
            if (ClassElementHasProductionHits(classElement))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ClassElementHasProductionHits(XElement classElement)
    {
        var filename = GetAttributeValue(classElement, "filename");
        var className = GetAttributeValue(classElement, "name");
        return IsProductionCoreClass(filename, className) && HasAnyLineHit(classElement);
    }

    private static string? GetAttributeValue(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }

    private static bool IsExcludedTestFilename(string lower)
    {
        return lower.Contains("/unittests/", StringComparison.Ordinal)
               || lower.Contains("/integrationtests/", StringComparison.Ordinal)
               || lower.Contains("/acceptancetests/", StringComparison.Ordinal);
    }

    private static bool IsCorePath(string lower)
    {
        return lower.Contains("/src/core/", StringComparison.Ordinal)
               || lower.StartsWith("src/core/", StringComparison.Ordinal)
               || lower.StartsWith("core/", StringComparison.Ordinal);
    }

    private static bool HasAnyLineHit(XElement scope)
    {
        foreach (var line in scope.Descendants("line"))
        {
            if (LineHasHits(line))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LineHasHits(XElement line)
    {
        var hitsText = GetAttributeValue(line, "hits");
        return int.TryParse(hitsText, out var hits) && hits > 0;
    }
}
