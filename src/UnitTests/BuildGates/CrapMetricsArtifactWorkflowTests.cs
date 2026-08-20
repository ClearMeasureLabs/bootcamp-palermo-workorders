using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapMetricsArtifactWorkflowTests
{
    [Test]
    public void BuildWorkflow_WhenRead_AppendsCrapSummaryToJobSummaryAfterEnforceStep()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));

        var enforceIndex = yaml.IndexOf("name: Enforce CRAP", StringComparison.Ordinal);
        var summaryIndex = yaml.IndexOf("name: Publish CRAP summary to job summary", StringComparison.Ordinal);
        enforceIndex.ShouldBeGreaterThan(-1);
        summaryIndex.ShouldBeGreaterThan(enforceIndex);

        var nextStepMarker = "\n      - name:";
        var nextStep = yaml.IndexOf(nextStepMarker, summaryIndex, StringComparison.Ordinal);
        nextStep.ShouldBeGreaterThan(summaryIndex);
        var summaryBlock = yaml.Substring(summaryIndex, nextStep - summaryIndex);

        summaryBlock.ShouldContain("if: always()");
        summaryBlock.ShouldContain("GITHUB_STEP_SUMMARY");
        summaryBlock.ShouldContain("crap-metrics/crap-summary.md");
        summaryBlock.ShouldNotContain("uses: actions/upload-artifact");
        summaryBlock.ShouldNotContain("crap-metrics-linux");

        yaml.ShouldNotContain("name: Upload CRAP metrics");
        yaml.ShouldNotContain("crap-metrics-linux");
    }

    [Test]
    public void CrapSkill_WhenRead_DocumentsJobSummaryNotZipArtifact()
    {
        var skill = File.ReadAllText(FindRepoFile(Path.Combine(
            ".cursor", "skills", "crap-score-cleanup", "SKILL.md")));

        skill.ShouldContain("GITHUB_STEP_SUMMARY");
        skill.ShouldContain("job summary");
        skill.ShouldContain("crap-metrics/crap-summary.md");
        skill.ShouldContain("Integration Build (SQL container)");
        skill.ShouldContain("Publish CRAP summary to job summary");
        skill.ShouldContain("if: always()");
        skill.ShouldNotContain("crap-metrics-linux");
        skill.ShouldNotContain("upload-artifact");
        skill.ShouldNotContain("Unzip");
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
