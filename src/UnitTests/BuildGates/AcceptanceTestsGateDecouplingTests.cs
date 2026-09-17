using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class AcceptanceTestsGateDecouplingTests
{
    [Test]
    public void BuildWorkflow_AcceptanceTestsJob_IfExpression_DoesNotContainSuccess()
    {
        var job = ReadJob("  acceptance-tests:", "  acceptance-tests-arm:");
        var ifLine = ExtractIfLine(job);

        ifLine.ShouldNotContain("success()");
        ifLine.ShouldContain("needs.changes.outputs.code == 'true'");
    }

    [Test]
    public void BuildWorkflow_AcceptanceTestsArmJob_IfExpression_DoesNotContainSuccess()
    {
        var job = ReadJob("  acceptance-tests-arm:", "  build-result:");
        var ifLine = ExtractIfLine(job);

        ifLine.ShouldNotContain("success()");
        ifLine.ShouldContain("needs.changes.outputs.code == 'true'");
    }

    [Test]
    public void BuildWorkflow_BuildResultJob_ListsEveryGatedJobInNeedsAndChecks()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));
        var jobStart = yaml.IndexOf("  build-result:", StringComparison.Ordinal);
        jobStart.ShouldBeGreaterThan(-1);
        var buildResultJob = yaml.Substring(jobStart);

        var gatedJobs = new (string Job, string EnvVar)[]
        {
            ("build-linux", "LINUX"),
            ("build-sqlite", "SQLITE"),
            ("integration-build-arm", "ARM"),
            ("code-analysis", "ANALYSIS"),
            ("qodana", "QODANA"),
            ("build-windows", "WINDOWS"),
            ("acceptance-tests", "ACCEPTANCE"),
            ("acceptance-tests-arm", "ACCEPTANCE_ARM"),
        };

        foreach (var (job, envVar) in gatedJobs)
        {
            buildResultJob.ShouldContain($"- {job}");
            buildResultJob.ShouldContain($"{envVar}: ${{{{ needs.{job}.result }}}}");
            buildResultJob.ShouldContain($"\"${{{envVar}}}\"");
        }

        buildResultJob.ShouldContain("if: always() && !cancelled()");
    }

    private static string ExtractIfLine(string job)
    {
        var ifIndex = job.IndexOf("\n    if:", StringComparison.Ordinal);
        ifIndex.ShouldBeGreaterThan(-1);
        var lineEnd = job.IndexOf('\n', ifIndex + 1);
        return job.Substring(ifIndex, lineEnd - ifIndex);
    }

    private static string ReadJob(string jobMarker, string nextJobMarker)
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));
        var jobStart = yaml.IndexOf(jobMarker, StringComparison.Ordinal);
        jobStart.ShouldBeGreaterThan(-1);
        var nextJob = yaml.IndexOf(nextJobMarker, jobStart, StringComparison.Ordinal);
        nextJob.ShouldBeGreaterThan(jobStart);
        return yaml.Substring(jobStart, nextJob - jobStart);
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
