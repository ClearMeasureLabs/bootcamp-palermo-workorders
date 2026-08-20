#!/usr/bin/env dotnet-script
#r "nuget: System.Text.Json, 9.0.0"

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

if (Args.Count < 2)
{
    Console.Error.WriteLine("Usage: dotnet script rollup-file-scores.csx -- <crap-report.json> <output-dir>");
    Environment.Exit(1);
}

var reportPath = Path.GetFullPath(Args[0]);
var outputDir = Path.GetFullPath(Args[1]);
Directory.CreateDirectory(outputDir);

var json = await File.ReadAllTextAsync(reportPath);
var report = JsonSerializer.Deserialize<CrapReport>(json, JsonOptions)
    ?? throw new InvalidOperationException("Failed to parse CRAP report.");

var flattenedCoverage = Path.Combine(outputDir, "coverage.flattened.cobertura.xml");
if (File.Exists(flattenedCoverage))
{
    report = OverlayLineCoverage(report, flattenedCoverage);
}

var threshold = report.Threshold;
var files = report.Methods
    .Where(m => !string.IsNullOrWhiteSpace(m.FilePath))
    .GroupBy(m => NormalizePath(m.FilePath))
    .Select(g =>
    {
        var methods = g.ToList();
        var crappy = methods.Where(m => m.Crap > threshold).ToList();
        return new FileScore
        {
            FilePath = g.Key,
            MethodCount = methods.Count,
            CrappyMethodCount = crappy.Count,
            MaxCrap = methods.Max(m => m.Crap),
            AvgCrap = Math.Round(methods.Average(m => m.Crap), 2),
            TotalCrapLoad = Math.Round(crappy.Sum(m => Math.Max(0, m.Crap - threshold)), 2),
            AvgCoverage = Math.Round(methods.Average(m => m.Coverage), 1),
            MaxComplexity = methods.Max(m => m.Complexity),
            WorstMethod = methods.OrderByDescending(m => m.Crap).First().FullName,
            IsProduction = IsProductionFile(g.Key)
        };
    })
    .OrderByDescending(f => f.MaxCrap)
    .ThenByDescending(f => f.TotalCrapLoad)
    .ToList();

var byFileJson = Path.Combine(outputDir, "crap-by-file.json");
var byFileCsv = Path.Combine(outputDir, "crap-by-file.csv");
var summaryMd = Path.Combine(outputDir, "crap-summary.md");
var violationsJson = Path.Combine(outputDir, "crap-production-violations.json");

var productionViolations = report.Methods
    .Where(m => m.Crap > threshold && IsProductionFile(m.FilePath))
    .OrderByDescending(m => m.Crap)
    .Select(m => new
    {
        fullName = m.FullName,
        filePath = m.FilePath,
        crap = m.Crap,
        complexity = m.Complexity,
        coverage = m.Coverage
    })
    .ToList();

await File.WriteAllTextAsync(byFileJson, JsonSerializer.Serialize(new
{
    schemaVersion = "1.0",
    generatedAt = DateTimeOffset.UtcNow,
    threshold,
    fileCount = files.Count,
    productionFileCount = files.Count(f => f.IsProduction),
    files
}, JsonOptions));

await File.WriteAllTextAsync(violationsJson, JsonSerializer.Serialize(new
{
    schemaVersion = "1.0",
    generatedAt = DateTimeOffset.UtcNow,
    threshold,
    violationCount = productionViolations.Count,
    methods = productionViolations
}, JsonOptions));

await WriteCsvAsync(byFileCsv, files);
await WriteSummaryAsync(summaryMd, report, files, threshold);

Console.WriteLine($"Wrote {files.Count} file scores to {outputDir}");
Console.WriteLine($"Production methods over threshold {threshold}: {productionViolations.Count}");

static string NormalizePath(string path) =>
    path.Replace('\\', '/').Trim();

static bool IsProductionFile(string path)
{
    var p = NormalizePath(path).ToLowerInvariant();
    if (p.Contains("/unittests/") || p.Contains("/integrationtests/") || p.Contains("/acceptancetests/"))
        return false;
    if (p.Contains("/generated/") || p.EndsWith(".g.cs") || p.EndsWith(".designer.cs"))
        return false;
    return p.Contains("/src/");
}

static CrapReport OverlayLineCoverage(CrapReport report, string coberturaPath)
{
    var hitsByFile = LoadCoberturaHits(coberturaPath);
    if (hitsByFile.Count == 0)
        return report;

    var methods = report.Methods
        .Select(m => OverlayMethod(m, report.Methods, hitsByFile))
        .ToList();
    return new CrapReport
    {
        Project = report.Project,
        Timestamp = report.Timestamp,
        Threshold = report.Threshold,
        Stats = report.Stats,
        Methods = methods
    };
}

static MethodScore OverlayMethod(
    MethodScore method,
    List<MethodScore> allMethods,
    Dictionary<string, Dictionary<int, int>> hitsByFile)
{
    var fileKey = CoverageFileKey(method.FilePath);
    if (string.IsNullOrEmpty(fileKey) || !hitsByFile.TryGetValue(fileKey, out var lineHits))
        return method;

    var start = method.LineNumber <= 0 ? 1 : method.LineNumber;
    var end = allMethods
        .Where(m => CoverageFileKey(m.FilePath) == fileKey && m.LineNumber > start)
        .Select(m => m.LineNumber)
        .DefaultIfEmpty(int.MaxValue)
        .Min();

    var lines = lineHits.Where(kv => kv.Key >= start && kv.Key < end).ToList();
    if (lines.Count == 0)
        return method;

    var covered = lines.Count(kv => kv.Value > 0);
    var coverage = 100.0 * covered / lines.Count;
    var crap = method.Complexity * method.Complexity * Math.Pow(1 - coverage / 100.0, 3) + method.Complexity;
    return new MethodScore
    {
        FullName = method.FullName,
        FilePath = method.FilePath,
        LineNumber = method.LineNumber,
        Crap = Math.Round(crap, 4),
        Complexity = method.Complexity,
        Coverage = Math.Round(coverage, 4)
    };
}

static Dictionary<string, Dictionary<int, int>> LoadCoberturaHits(string path)
{
    var doc = XDocument.Load(path);
    var result = new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);
    foreach (var classElement in doc.Descendants("class"))
    {
        var filenameAttr = classElement.Attribute("filename");
        var filename = filenameAttr == null ? "" : filenameAttr.Value;
        var key = CoverageFileKey(filename);
        if (string.IsNullOrEmpty(key))
            continue;

        if (!result.TryGetValue(key, out var lines))
        {
            lines = new Dictionary<int, int>();
            result[key] = lines;
        }

        var linesElement = classElement.Element("lines");
        var lineElements = linesElement == null
            ? Enumerable.Empty<XElement>()
            : linesElement.Elements("line");
        foreach (var line in lineElements)
        {
            var numberAttr = line.Attribute("number");
            var hitsAttr = line.Attribute("hits");
            if (numberAttr == null || !int.TryParse(numberAttr.Value, out var number))
                continue;
            var hits = hitsAttr != null && int.TryParse(hitsAttr.Value, out var h) ? h : 0;
            if (lines.TryGetValue(number, out var existing))
                lines[number] = Math.Max(existing, hits);
            else
                lines[number] = hits;
        }
    }

    return result;
}

static string CoverageFileKey(string path)
{
    var normalized = NormalizePath(path).TrimStart('/');
    var src = normalized.IndexOf("/src/", StringComparison.OrdinalIgnoreCase);
    if (src >= 0)
        normalized = normalized[(src + 5)..];
    return normalized.ToLowerInvariant();
}

static async Task WriteCsvAsync(string path, List<FileScore> files)
{
    var sb = new StringBuilder();
    sb.AppendLine("FilePath,MethodCount,CrappyMethodCount,MaxCrap,AvgCrap,TotalCrapLoad,AvgCoverage,MaxComplexity,WorstMethod,IsProduction");
    foreach (var f in files)
    {
        sb.AppendLine(string.Join(",",
            Csv(f.FilePath),
            f.MethodCount,
            f.CrappyMethodCount,
            f.MaxCrap.ToString(CultureInfo.InvariantCulture),
            f.AvgCrap.ToString(CultureInfo.InvariantCulture),
            f.TotalCrapLoad.ToString(CultureInfo.InvariantCulture),
            f.AvgCoverage.ToString(CultureInfo.InvariantCulture),
            f.MaxComplexity,
            Csv(f.WorstMethod),
            f.IsProduction));
    }
    await File.WriteAllTextAsync(path, sb.ToString());
}

static string Csv(string value) =>
    value.Contains(',') || value.Contains('"')
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

static async Task WriteSummaryAsync(string path, CrapReport report, List<FileScore> files, int threshold)
{
    var prod = files.Where(f => f.IsProduction).ToList();
    var prodMethods = report.Methods
        .Where(m => IsProductionFile(m.FilePath))
        .ToList();
    var methodCount = prodMethods.Count;
    var crappyMethods = prodMethods.Where(m => m.Crap > threshold).ToList();
    var crappyCount = crappyMethods.Count;
    var crappyPercent = methodCount == 0 ? 0.0 : 100.0 * crappyCount / methodCount;
    var averageCrap = methodCount == 0 ? 0.0 : prodMethods.Average(m => m.Crap);
    var medianCrap = Median(prodMethods.Select(m => m.Crap).ToList());
    var totalCrapLoad = crappyMethods.Sum(m => Math.Max(0, m.Crap - threshold));

    var sb = new StringBuilder();
    sb.AppendLine("# CRAP Score Summary");
    sb.AppendLine();
    sb.AppendLine($"**Project:** {report.Project}");
    sb.AppendLine($"**Generated:** {report.Timestamp:u}");
    sb.AppendLine($"**Threshold:** {threshold}");
    sb.AppendLine();
    sb.AppendLine("## Production gate stats");
    sb.AppendLine("_Out-of-scope paths (tests, `/Generated/`, `*.g.cs`, `*.Designer.cs`) are omitted from these stats._");
    sb.AppendLine($"- Methods analyzed: {methodCount}");
    sb.AppendLine($"- CRAPpy methods: {crappyCount} ({crappyPercent:F1}%)");
    sb.AppendLine($"- Average CRAP: {averageCrap:F1}");
    sb.AppendLine($"- Median CRAP: {medianCrap}");
    sb.AppendLine($"- Total CRAP load: {totalCrapLoad:F1}");
    sb.AppendLine();
    sb.AppendLine("## Top 20 production files by MaxCrap");
    sb.AppendLine();
    sb.AppendLine("| File | MaxCrap | Crappy | AvgCov% | Worst method |");
    sb.AppendLine("|------|---------|--------|---------|--------------|");
    foreach (var f in prod.Take(20))
    {
        var rel = f.FilePath.Contains("/src/") ? f.FilePath[f.FilePath.IndexOf("/src/", StringComparison.Ordinal)..] : f.FilePath;
        sb.AppendLine($"| `{rel}` | {f.MaxCrap:F1} | {f.CrappyMethodCount} | {f.AvgCoverage:F0} | {Truncate(f.WorstMethod, 60)} |");
    }
    sb.AppendLine();
    sb.AppendLine("## Cleanup queue (production, MaxCrap > threshold)");
    sb.AppendLine();
    var queue = prod.Where(f => f.MaxCrap > threshold).Take(15).ToList();
    if (queue.Count == 0)
    {
        sb.AppendLine("_No production files exceed the threshold._");
    }
    else
    {
        var i = 1;
        foreach (var f in queue)
        {
            sb.AppendLine($"{i}. **{f.FilePath}** — MaxCrap {f.MaxCrap:F1}, {f.CrappyMethodCount} CRAPpy method(s)");
            i++;
        }
    }
    await File.WriteAllTextAsync(path, sb.ToString());
}

static double Median(List<double> values)
{
    if (values.Count == 0)
        return 0;
    var sorted = values.OrderBy(v => v).ToList();
    var mid = sorted.Count / 2;
    return sorted.Count % 2 == 0
        ? (sorted[mid - 1] + sorted[mid]) / 2.0
        : sorted[mid];
}

static string Truncate(string s, int max) =>
    s.Length <= max ? s : s[..(max - 3)] + "...";

static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

record CrapReport
{
    public string Project { get; init; } = "";
    public DateTimeOffset Timestamp { get; init; }
    public int Threshold { get; init; }
    public CrapStats Stats { get; init; } = new();
    public List<MethodScore> Methods { get; init; } = [];
}

record CrapStats
{
    public int MethodCount { get; init; }
    public double AverageCrap { get; init; }
    public double MedianCrap { get; init; }
    public int CrappyMethodCount { get; init; }
    public double CrappyMethodPercent { get; init; }
    public double TotalCrapLoad { get; init; }
}

record MethodScore
{
    public string FullName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public int LineNumber { get; init; }
    public double Crap { get; init; }
    public int Complexity { get; init; }
    public double Coverage { get; init; }
}

record FileScore
{
    public string FilePath { get; init; } = "";
    public int MethodCount { get; init; }
    public int CrappyMethodCount { get; init; }
    public double MaxCrap { get; init; }
    public double AvgCrap { get; init; }
    public double TotalCrapLoad { get; init; }
    public double AvgCoverage { get; init; }
    public int MaxComplexity { get; init; }
    public string WorstMethod { get; init; } = "";
    public bool IsProduction { get; init; }
}
