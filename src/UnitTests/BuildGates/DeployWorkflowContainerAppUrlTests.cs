using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class DeployWorkflowContainerAppUrlTests
{
    [Test]
    public void DeployWorkflow_UatJob_WhenRead_PrintsContainerAppUrlAfterAwait()
    {
        var uatJob = ReadJob("  deploy-to-uat:", "  deploy-to-prod:");

        var awaitIndex = uatJob.IndexOf("Wait for the UAT deployment to complete", StringComparison.Ordinal);
        awaitIndex.ShouldBeGreaterThan(-1);

        var loginIndex = uatJob.IndexOf("azure/login@v2", awaitIndex, StringComparison.Ordinal);
        loginIndex.ShouldBeGreaterThan(awaitIndex);

        var fqdnStepIndex = uatJob.IndexOf("Get Container App FQDN", awaitIndex, StringComparison.Ordinal);
        fqdnStepIndex.ShouldBeGreaterThan(loginIndex);

        var fqdnBlock = ExtractStepBlock(uatJob, fqdnStepIndex);
        fqdnBlock.ShouldContain("az containerapp show");
        fqdnBlock.ShouldContain("vars.CONTAINER_APP_NAME");
        fqdnBlock.ShouldContain("vars.UAT_RESOURCE_GROUP_NAME");
        fqdnBlock.ShouldContain("properties.configuration.ingress.fqdn");
        fqdnBlock.ShouldContain("Container App URL:");
        fqdnBlock.ShouldNotContain(".azurecontainerapps.io");
    }

    [Test]
    public void DeployWorkflow_ProdJob_WhenRead_PrintsContainerAppUrlAfterAwait()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
        var prodStart = yaml.IndexOf("  deploy-to-prod:", StringComparison.Ordinal);
        prodStart.ShouldBeGreaterThan(-1);
        var prodJob = yaml.Substring(prodStart);

        var awaitIndex = prodJob.IndexOf("Wait for the Prod deployment to complete", StringComparison.Ordinal);
        awaitIndex.ShouldBeGreaterThan(-1);

        var loginIndex = prodJob.IndexOf("azure/login@v2", awaitIndex, StringComparison.Ordinal);
        loginIndex.ShouldBeGreaterThan(awaitIndex);

        var fqdnStepIndex = prodJob.IndexOf("Get Container App FQDN", awaitIndex, StringComparison.Ordinal);
        fqdnStepIndex.ShouldBeGreaterThan(loginIndex);

        var fqdnBlock = ExtractStepBlock(prodJob, fqdnStepIndex);
        fqdnBlock.ShouldContain("az containerapp show");
        fqdnBlock.ShouldContain("vars.CONTAINER_APP_NAME");
        fqdnBlock.ShouldContain("vars.PROD_RESOURCE_GROUP_NAME");
        fqdnBlock.ShouldContain("properties.configuration.ingress.fqdn");
        fqdnBlock.ShouldContain("Container App URL:");
        fqdnBlock.ShouldNotContain(".azurecontainerapps.io");
    }

    [Test]
    public void DeployWorkflow_UrlSteps_WhenRead_MatchFactoryParserPrefix()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
        var uatJob = ReadJob("  deploy-to-uat:", "  deploy-to-prod:");
        var prodStart = yaml.IndexOf("  deploy-to-prod:", StringComparison.Ordinal);
        var prodJob = yaml.Substring(prodStart);

        const string factoryPrefixLine = "Write-Host \"Container App URL: $containerAppUrl\"";
        uatJob.ShouldContain(factoryPrefixLine);
        prodJob.ShouldContain(factoryPrefixLine);

        var tddStart = yaml.IndexOf("  deploy-to-tdd:", StringComparison.Ordinal);
        var uatStart = yaml.IndexOf("  deploy-to-uat:", StringComparison.Ordinal);
        tddStart.ShouldBeGreaterThan(-1);
        uatStart.ShouldBeGreaterThan(tddStart);
        yaml.Substring(tddStart, uatStart - tddStart).ShouldContain(factoryPrefixLine);
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

    private static string ExtractStepBlock(string jobYaml, int stepNameIndex)
    {
        var nextStepMarker = "\n      - name:";
        var nextStep = jobYaml.IndexOf(nextStepMarker, stepNameIndex, StringComparison.Ordinal);
        if (nextStep < 0)
        {
            return jobYaml.Substring(stepNameIndex);
        }

        return jobYaml.Substring(stepNameIndex, nextStep - stepNameIndex);
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
