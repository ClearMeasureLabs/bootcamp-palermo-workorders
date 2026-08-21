// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable NotAccessedPositionalProperty.Local
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Counts in-scope production methods whose CRAP score exceeds a threshold.
/// </summary>
public static class CrapGateEvaluator
{
    /// <summary>
    /// Evaluates a crap4dotnet <c>crap-report.json</c> document.
    /// </summary>
    public static CrapGateResult EvaluateReport(string reportJson, int threshold)
    {
        using var document = JsonDocument.Parse(reportJson);
        var methods = new List<CrapMethodViolation>();
        if (!TryGetMethodsArray(document.RootElement, out var methodsElement))
        {
            return new CrapGateResult(threshold, 0, methods);
        }

        foreach (var method in methodsElement.EnumerateArray())
        {
            if (TryCreateViolation(method, threshold, out var violation))
            {
                methods.Add(violation);
            }
        }

        methods.Sort((left, right) => right.Crap.CompareTo(left.Crap));
        return new CrapGateResult(threshold, methods.Count, methods);
    }

    private static bool TryGetMethodsArray(JsonElement root, out JsonElement methodsElement)
    {
        if (root.TryGetProperty("methods", out methodsElement)
            && methodsElement.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        methodsElement = default;
        return false;
    }

    private static bool TryCreateViolation(JsonElement method, int threshold, out CrapMethodViolation violation)
    {
        var filePath = ReadString(method, "filePath");
        var crap = ReadDouble(method, "crap");
        if (crap <= threshold || !CrapProductionScope.IsProductionFile(filePath))
        {
            violation = null!;
            return false;
        }

        violation = new CrapMethodViolation(
            ReadString(method, "fullName") ?? "",
            filePath ?? "",
            crap,
            ReadInt(method, "complexity"),
            ReadDouble(method, "coverage"));
        return true;
    }

    private static string? ReadString(JsonElement method, string propertyName)
    {
        return method.TryGetProperty(propertyName, out var element)
            ? element.GetString()
            : null;
    }

    private static double ReadDouble(JsonElement method, string propertyName)
    {
        return method.TryGetProperty(propertyName, out var element)
            ? element.GetDouble()
            : 0;
    }

    private static int ReadInt(JsonElement method, string propertyName)
    {
        return method.TryGetProperty(propertyName, out var element)
            ? element.GetInt32()
            : 0;
    }
}

/// <summary>
/// Outcome of a CRAP production-scope gate evaluation.
/// </summary>
public sealed record CrapGateResult(int Threshold, int ViolationCount, IReadOnlyList<CrapMethodViolation> Methods);

/// <summary>
/// One production method over the CRAP threshold.
/// </summary>
public sealed record CrapMethodViolation(
    string FullName,
    string FilePath,
    double Crap,
    int Complexity,
    double Coverage);
