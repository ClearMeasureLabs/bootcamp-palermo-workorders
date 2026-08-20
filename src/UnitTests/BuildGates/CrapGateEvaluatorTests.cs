using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapGateEvaluatorTests
{
    [Test]
    public void EvaluateReport_WhenOnlyTestsAndGeneratedExceedThreshold_ReturnsZero()
    {
        var result = CrapGateEvaluator.EvaluateReport(PassFixture, threshold: 15);

        result.ViolationCount.ShouldBe(0);
        result.Methods.ShouldBeEmpty();
    }

    [Test]
    public void EvaluateReport_WhenProductionMethodExceedsThreshold_ReturnsThatMethod()
    {
        var result = CrapGateEvaluator.EvaluateReport(FailFixture, threshold: 15);

        result.ViolationCount.ShouldBe(1);
        result.Methods[0].FullName.ShouldBe("ClearMeasure.Bootcamp.Core.Import.WorkOrderBulkImportCsvParser.Parse");
        result.Methods[0].Crap.ShouldBe(20);
    }

    [Test]
    public void EvaluateReport_WhenThresholdRaisedAboveProductionCrap_ReturnsZero()
    {
        CrapGateEvaluator.EvaluateReport(FailFixture, threshold: 20).ViolationCount.ShouldBe(0);
    }

    [Test]
    public void RollupScript_WhenRead_ContainsProductionExclusionTokens()
    {
        var scriptPath = FindRollupScript();
        var source = File.ReadAllText(scriptPath);

        source.ShouldContain("IsProductionFile");
        source.ShouldContain("/unittests/");
        source.ShouldContain("/integrationtests/");
        source.ShouldContain("/acceptancetests/");
        source.ShouldContain("/generated/");
        source.ShouldContain(".g.cs");
        source.ShouldContain(".designer.cs");
        source.ShouldContain("/src/");
        source.ShouldContain("crap-production-violations.json");
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

    private const string PassFixture = """
        {
          "threshold": 15,
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

    private const string FailFixture = """
        {
          "threshold": 15,
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
