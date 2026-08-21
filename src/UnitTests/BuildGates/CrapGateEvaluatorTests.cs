using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapGateEvaluatorTests
{
    [Test]
    public void EvaluateReport_WhenOnlyTestsAndGeneratedExceedThreshold_ReturnsZero()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var result = CrapGateEvaluator.EvaluateReport(PassFixture(gate), threshold: gate);

        result.ViolationCount.ShouldBe(0);
        result.Methods.ShouldBeEmpty();
    }

    [Test]
    public void EvaluateReport_WhenProductionMethodExceedsThreshold_ReturnsThatMethod()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var result = CrapGateEvaluator.EvaluateReport(FailFixture(gate), threshold: gate);

        result.ViolationCount.ShouldBe(1);
        result.Methods[0].FullName.ShouldBe("ClearMeasure.Bootcamp.Core.Import.WorkOrderBulkImportCsvParser.Parse");
        result.Methods[0].Crap.ShouldBe(20);
    }

    [Test]
    public void EvaluateReport_WhenWindowsBackslashProductionPathExceedsThreshold_CountsViolation()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var json = $$"""
            {
              "threshold": {{gate}},
              "methods": [
                {
                  "fullName": "ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderCommandExecutor.ExecuteCommandAsync",
                  "filePath": "D:\\\\bootcamp-palermo-workorders\\\\src\\\\McpServer\\\\Tools\\\\WorkOrderCommandExecutor.cs",
                  "crap": 56,
                  "complexity": 7,
                  "coverage": 0
                }
              ]
            }
            """;

        CrapGateEvaluator.EvaluateReport(json, threshold: gate).ViolationCount.ShouldBe(1);
    }

    [Test]
    public void EvaluateReport_WhenThresholdRaisedAboveProductionCrap_ReturnsZero()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        CrapGateEvaluator.EvaluateReport(FailFixture(gate), threshold: 20).ViolationCount.ShouldBe(0);
    }

    [Test]
    public void EvaluateReport_WhenProductionCrapIsJustOverGate_RejectsAtGateAndAcceptsAtGatePlusOne()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var justOver = gate + 1;
        var json = $$"""
            {
              "threshold": {{gate}},
              "methods": [
                {
                  "fullName": "ClearMeasure.Bootcamp.Core.Model.WorkOrder.ChangeStatus",
                  "filePath": "D:/repo/src/Core/Model/WorkOrder.cs",
                  "crap": {{justOver}},
                  "complexity": {{justOver}},
                  "coverage": 100
                }
              ]
            }
            """;

        CrapGateEvaluator.EvaluateReport(json, threshold: gate).ViolationCount.ShouldBe(1);
        CrapGateEvaluator.EvaluateReport(json, threshold: justOver).ViolationCount.ShouldBe(0);
    }

    [Test]
    public void RollupScript_WhenRead_ContainsProductionExclusionTokens()
    {
        var scriptPath = FindRollupScript();
        var source = File.ReadAllText(scriptPath);

        source.ShouldContain("IsProductionFile");
        source.ShouldContain("NormalizePath(path)");
        source.ShouldContain("/unittests/");
        source.ShouldContain("/integrationtests/");
        source.ShouldContain("/acceptancetests/");
        source.ShouldContain("/generated/");
        source.ShouldContain(".g.cs");
        source.ShouldContain(".designer.cs");
        source.ShouldContain("/src/");
        source.ShouldContain("crap-production-violations.json");
        source.ShouldContain("OverlayLineCoverage");
        source.ShouldContain("coverage.flattened.cobertura.xml");
    }

    [Test]
    public void RollupScript_WriteSummary_UsesProductionGateStatsNotWholeSolutionStats()
    {
        var source = File.ReadAllText(FindRollupScript());
        var writeSummaryStart = source.IndexOf("static async Task WriteSummaryAsync", StringComparison.Ordinal);
        writeSummaryStart.ShouldBeGreaterThan(-1);
        var writeSummaryEnd = source.IndexOf("static double Median", writeSummaryStart, StringComparison.Ordinal);
        writeSummaryEnd.ShouldBeGreaterThan(writeSummaryStart);
        var writeSummary = source.Substring(writeSummaryStart, writeSummaryEnd - writeSummaryStart);

        writeSummary.ShouldContain("## Production gate stats");
        writeSummary.ShouldContain("Out-of-scope paths");
        writeSummary.ShouldContain("IsProductionFile(m.FilePath)");
        writeSummary.ShouldNotContain("## Solution stats");
        writeSummary.ShouldNotContain("report.Stats.MethodCount");
        writeSummary.ShouldNotContain("report.Stats.CrappyMethodCount");
        writeSummary.ShouldNotContain("report.Stats.AverageCrap");
        writeSummary.ShouldNotContain("report.Stats.MedianCrap");
        writeSummary.ShouldNotContain("report.Stats.TotalCrapLoad");
    }

    [Test]
    public void AuditScript_WhenRead_PrependsDotnetToolsToPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName,
                ".cursor", "skills", "crap-score-cleanup", "scripts", "run-crap-audit.ps1");
            if (File.Exists(candidate))
            {
                var source = File.ReadAllText(candidate);
                source.ShouldContain("Join-Path $HOME \".dotnet\" \"tools\"");
                source.ShouldContain("$env:PATH = \"$dotnetTools");
                source.ShouldContain("dotnet-script\" -Command \"dotnet-script\" -Version \"2.0.0\"");
                source.ShouldContain("Failed to roll up CRAP scores");
                return;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("run-crap-audit.ps1 not found from test directory.");
    }

    [Test]
    public void FlattenScript_WhenRead_ContainsAsyncStateMachinePattern()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName,
                ".cursor", "skills", "crap-score-cleanup", "scripts", "flatten-cobertura.csx");
            if (File.Exists(candidate))
            {
                var source = File.ReadAllText(candidate);
                source.ShouldContain("StateMachineClassName");
                source.ShouldContain("FlattenAndMerge");
                source.ShouldContain("<(?<method>[^>]+)>d__");
                return;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("flatten-cobertura.csx not found from test directory.");
    }

    private static string FindRollupScript()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName,
                ".cursor", "skills", "crap-score-cleanup", "scripts", "rollup-file-scores.csx");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("rollup-file-scores.csx not found from test directory.");
    }

    private static string PassFixture(int threshold) => $$"""
        {
          "threshold": {{threshold}},
          "methods": [
            {
              "fullName": "ClearMeasure.Bootcamp.Core.Model.WorkOrder.get_Title",
              "filePath": "D:/repo/src/Core/Model/WorkOrder.cs",
              "crap": 2,
              "complexity": 1,
              "coverage": 100
            },
            {
              "fullName": "ClearMeasure.Bootcamp.UnitTests.Foo.Bar",
              "filePath": "D:/repo/src/UnitTests/Foo.cs",
              "crap": 420,
              "complexity": 20,
              "coverage": 0
            },
            {
              "fullName": "ClearMeasure.Bootcamp.UI.Server.Generated.Workorders.MergeFrom",
              "filePath": "D:/repo/src/UI/Server/Generated/Protos/Workorders.cs",
              "crap": 272,
              "complexity": 16,
              "coverage": 0
            }
          ]
        }
        """;

    private static string FailFixture(int threshold) => $$"""
        {
          "threshold": {{threshold}},
          "methods": [
            {
              "fullName": "ClearMeasure.Bootcamp.Core.Import.WorkOrderBulkImportCsvParser.Parse",
              "filePath": "D:/repo/src/Core/Import/WorkOrderBulkImportCsvParser.cs",
              "crap": 20,
              "complexity": 4,
              "coverage": 0
            },
            {
              "fullName": "ClearMeasure.Bootcamp.AcceptanceTests.ToTestDateTime",
              "filePath": "D:/repo/src/AcceptanceTests/Extensions/DateTimeTestExtensions.cs",
              "crap": 420,
              "complexity": 20,
              "coverage": 0
            }
          ]
        }
        """;
}
