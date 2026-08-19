#!/usr/bin/env dotnet-script
#r "nuget: System.Text.Json, 9.0.0"

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

await File.WriteAllTextAsync(byFileJson, JsonSerializer.Serialize(new
{
    schemaVersion = "1.0",
    generatedAt = DateTimeOffset.UtcNow,
    threshold,
    fileCount = files.Count,
    productionFileCount = files.Count(f => f.IsProduction),
    files
}, JsonOptions));

await WriteCsvAsync(byFileCsv, files);
await WriteSummaryAsync(summaryMd, report, files, threshold);

Console.WriteLine($"Wrote {files.Count} file scores to {outputDir}");

static string NormalizePath(string path) =>
    path.Replace('\\', '/').Trim();

static bool IsProductionFile(string path)
{
    var p = path.ToLowerInvariant();
    if (p.Contains("/unittests/") || p.Contains("/integrationtests/") || p.Contains("/acceptancetests/"))
        return false;
    if (p.Contains("/generated/") || p.EndsWith(".g.cs") || p.EndsWith(".designer.cs"))
        return false;
    return p.Contains("/src/");
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
    var sb = new StringBuilder();
    sb.AppendLine("# CRAP Score Summary");
    sb.AppendLine();
    sb.AppendLine($"**Project:** {report.Project}");
    sb.AppendLine($"**Generated:** {report.Timestamp:u}");
    sb.AppendLine($"**Threshold:** {threshold}");
    sb.AppendLine();
    sb.AppendLine("## Solution stats");
    sb.AppendLine($"- Methods analyzed: {report.Stats.MethodCount}");
    sb.AppendLine($"- CRAPpy methods: {report.Stats.CrappyMethodCount} ({report.Stats.CrappyMethodPercent:F1}%)");
    sb.AppendLine($"- Average CRAP: {report.Stats.AverageCrap:F1}");
    sb.AppendLine($"- Median CRAP: {report.Stats.MedianCrap}");
    sb.AppendLine($"- Total CRAP load: {report.Stats.TotalCrapLoad:F1}");
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
