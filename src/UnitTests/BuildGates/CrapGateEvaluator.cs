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
        if (!document.RootElement.TryGetProperty("methods", out var methodsElement)
            || methodsElement.ValueKind != JsonValueKind.Array)
        {
            return new CrapGateResult(threshold, 0, methods);
        }

        foreach (var method in methodsElement.EnumerateArray())
        {
            var filePath = method.TryGetProperty("filePath", out var filePathElement)
                ? filePathElement.GetString()
                : null;
            var crap = method.TryGetProperty("crap", out var crapElement)
                ? crapElement.GetDouble()
                : 0;
            if (crap <= threshold || !CrapProductionScope.IsProductionFile(filePath))
            {
                continue;
            }

            methods.Add(new CrapMethodViolation(
                method.TryGetProperty("fullName", out var nameElement) ? nameElement.GetString() ?? "" : "",
                filePath ?? "",
                crap,
                method.TryGetProperty("complexity", out var complexityElement) ? complexityElement.GetInt32() : 0,
                method.TryGetProperty("coverage", out var coverageElement) ? coverageElement.GetDouble() : 0));
        }

        methods.Sort((left, right) => right.Crap.CompareTo(left.Crap));
        return new CrapGateResult(threshold, methods.Count, methods);
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
