using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapMetricsArtifactWorkflowTests
{
    [Test]
    public void BuildWorkflow_WhenRead_UploadsCrapMetricsAfterEnforceStep()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));

        var enforceIndex = yaml.IndexOf("name: Enforce CRAP", StringComparison.Ordinal);
        var uploadIndex = yaml.IndexOf("name: Upload CRAP metrics", StringComparison.Ordinal);
        enforceIndex.ShouldBeGreaterThan(-1);
        uploadIndex.ShouldBeGreaterThan(enforceIndex);

        var uploadBlock = yaml.Substring(uploadIndex);
        var nextJob = uploadBlock.IndexOf("\n  build-sqlite:", StringComparison.Ordinal);
        if (nextJob > 0)
        {
            uploadBlock = uploadBlock.Substring(0, nextJob);
        }

        uploadBlock.ShouldContain("uses: actions/upload-artifact@v4");
        uploadBlock.ShouldContain("if: always()");
        uploadBlock.ShouldContain("name: crap-metrics-linux");
        uploadBlock.ShouldContain("crap-metrics/crap-summary.md");
        uploadBlock.ShouldContain("crap-metrics/crap-report.json");
        uploadBlock.ShouldContain("crap-metrics/crap-by-file.json");
        uploadBlock.ShouldContain("crap-metrics/crap-by-file.csv");
        uploadBlock.ShouldContain("crap-metrics/crap-production-violations.json");
    }

    [Test]
    public void CrapSkill_WhenRead_DocumentsLinuxArtifactDownload()
    {
        var skill = File.ReadAllText(FindRepoFile(Path.Combine(
            ".cursor", "skills", "crap-score-cleanup", "SKILL.md")));

        skill.ShouldContain("crap-metrics-linux");
        skill.ShouldContain("Integration Build (SQL container)");
        skill.ShouldContain("Upload CRAP metrics");
        skill.ShouldContain("if: always()");
        skill.ShouldContain("Artifacts");
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
