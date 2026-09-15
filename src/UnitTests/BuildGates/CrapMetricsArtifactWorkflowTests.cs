using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapMetricsArtifactWorkflowTests
{
    [Test]
    public void BuildWorkflow_WhenRead_PublishesCrapSummaryAndArtifactAfterEnforceStep()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));

        var jobStart = yaml.IndexOf("  build-linux:", StringComparison.Ordinal);
        jobStart.ShouldBeGreaterThan(-1);
        var nextJob = yaml.IndexOf("\n  build-sqlite:", jobStart, StringComparison.Ordinal);
        nextJob.ShouldBeGreaterThan(jobStart);
        var linuxJob = yaml.Substring(jobStart, nextJob - jobStart);

        var enforceIndex = linuxJob.IndexOf("name: Enforce CRAP", StringComparison.Ordinal);
        var summaryIndex = linuxJob.IndexOf("name: Publish CRAP summary to job summary", StringComparison.Ordinal);
        enforceIndex.ShouldBeGreaterThan(-1);
        summaryIndex.ShouldBeGreaterThan(enforceIndex);

        var nextStepMarker = "\n      - name:";
        var nextStep = linuxJob.IndexOf(nextStepMarker, summaryIndex, StringComparison.Ordinal);
        nextStep.ShouldBeGreaterThan(summaryIndex);
        var summaryBlock = linuxJob.Substring(summaryIndex, nextStep - summaryIndex);

        summaryBlock.ShouldContain("if: always()");
        summaryBlock.ShouldContain("GITHUB_STEP_SUMMARY");
        summaryBlock.ShouldContain("crap-metrics/crap-summary.md");
        summaryBlock.ShouldNotContain("uses: actions/upload-artifact");

        var uploadIndex = linuxJob.IndexOf("name: Upload CRAP metrics", StringComparison.Ordinal);
        uploadIndex.ShouldBeGreaterThan(summaryIndex);
        var uploadEnd = linuxJob.IndexOf(nextStepMarker, uploadIndex, StringComparison.Ordinal);
        uploadEnd.ShouldBeGreaterThan(uploadIndex);
        var uploadBlock = linuxJob.Substring(uploadIndex, uploadEnd - uploadIndex);

        uploadBlock.ShouldContain("uses: actions/upload-artifact");
        uploadBlock.ShouldContain("if: always()");
        uploadBlock.ShouldContain("name: crap-metrics-linux");
        uploadBlock.ShouldContain("crap-metrics/crap-summary.md");
        uploadBlock.ShouldContain("crap-metrics/crap-report.json");
        uploadBlock.ShouldContain("crap-metrics/crap-by-file.json");
        uploadBlock.ShouldContain("crap-metrics/crap-by-file.csv");
        uploadBlock.ShouldContain("crap-metrics/crap-production-violations.json");
    }

    [Test]
    public void CrapAuditDoc_WhenRead_DocumentsJobSummaryAndArtifact()
    {
        var doc = File.ReadAllText(FindRepoFile(Path.Combine(
            "docs", "crap-score-audit.md")));

        doc.ShouldContain("GITHUB_STEP_SUMMARY");
        doc.ShouldContain("job summary");
        doc.ShouldContain("crap-metrics/crap-summary.md");
        doc.ShouldContain("Integration Build (SQL container)");
        doc.ShouldContain("Publish CRAP summary to job summary");
        doc.ShouldContain("Upload CRAP metrics");
        doc.ShouldContain("crap-metrics-linux");
        doc.ShouldContain("if: always()");
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
