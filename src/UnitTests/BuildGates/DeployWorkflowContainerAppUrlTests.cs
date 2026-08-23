using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Guards the Deploy workflow shape after the UAT/Prod FQDN lookup removal:
/// UAT and Prod end at the Octopus await-task gate with no Azure FQDN lookup
/// or URL-based health check, while TDD retains full URL verification.
/// </summary>
[TestFixture]
public class DeployWorkflowContainerAppUrlTests
{
    [Test]
    public void DeployWorkflow_UatJob_WhenRead_HasNoFqdnLookupAfterAwait()
    {
        var uatJob = ReadJob("  deploy-to-uat:", "  deploy-to-prod:");

        uatJob.IndexOf("Wait for the UAT deployment to complete", StringComparison.Ordinal)
            .ShouldBeGreaterThan(-1);
        uatJob.ShouldNotContain("Get Container App FQDN");
        uatJob.ShouldNotContain("az containerapp show");
        uatJob.ShouldNotContain("Wait for Container App to become healthy");
        uatJob.ShouldNotContain("azure/login@v2");
    }

    [Test]
    public void DeployWorkflow_ProdJob_WhenRead_HasNoFqdnLookupAfterAwait()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
        var prodStart = yaml.IndexOf("  deploy-to-prod:", StringComparison.Ordinal);
        prodStart.ShouldBeGreaterThan(-1);
        var prodJob = yaml.Substring(prodStart);

        prodJob.IndexOf("Wait for the Prod deployment to complete", StringComparison.Ordinal)
            .ShouldBeGreaterThan(-1);
        prodJob.ShouldNotContain("Get Container App FQDN");
        prodJob.ShouldNotContain("az containerapp show");
        prodJob.ShouldNotContain("Wait for Container App to become healthy");
        prodJob.ShouldNotContain("azure/login@v2");
    }

    [Test]
    public void DeployWorkflow_TddJob_WhenRead_RetainsUrlVerification()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
        var tddStart = yaml.IndexOf("  deploy-to-tdd:", StringComparison.Ordinal);
        var uatStart = yaml.IndexOf("  deploy-to-uat:", StringComparison.Ordinal);
        tddStart.ShouldBeGreaterThan(-1);
        uatStart.ShouldBeGreaterThan(tddStart);
        var tddJob = yaml.Substring(tddStart, uatStart - tddStart);

        tddJob.ShouldContain("Get Container App FQDN");
        tddJob.ShouldContain("Wait for Container App to become healthy");
        tddJob.ShouldContain("Write-Host \"Container App URL: $containerAppUrl\"");
    }

    private static string ReadJob(string jobMarker, string nextJobMarker)
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
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
