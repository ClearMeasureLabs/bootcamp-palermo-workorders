using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class DeployWorkflowPlaywrightCacheTests
{
    [Test]
    public void DeployWorkflow_PlaywrightCache_WhenRead_ScopesKeysByRunnerArch()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
        var tddJob = ReadTddJob(yaml);
        var cacheStepIndex = tddJob.IndexOf("Cache Playwright browsers", StringComparison.Ordinal);
        cacheStepIndex.ShouldBeGreaterThan(-1);

        var cacheStep = ExtractStepBlock(tddJob, cacheStepIndex);
        cacheStep.ShouldContain("playwright-${{ runner.os }}-${{ runner.arch }}-${{ hashFiles('src/AcceptanceTests/AcceptanceTests.csproj') }}");
        cacheStep.ShouldContain("playwright-${{ runner.os }}-${{ runner.arch }}-");
        cacheStep.ShouldNotContain("playwright-${{ runner.os }}-${{ hashFiles");
        cacheStep.ShouldNotContain("playwright-${{ runner.os }}-\n");
    }

    [Test]
    public void DeployWorkflow_PlaywrightInstall_WhenCacheMiss_ClearsCacheBeforeInstall()
    {
        var yaml = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "deploy.yml")));
        var tddJob = ReadTddJob(yaml);
        const string installStepMarker = "- name: Install Playwright";
        var installIndex = tddJob.IndexOf(installStepMarker, StringComparison.Ordinal);
        installIndex.ShouldBeGreaterThan(-1);

        // Prefer the cache-miss install step, not "Install Playwright dependencies only".
        var depsOnlyIndex = tddJob.IndexOf("- name: Install Playwright dependencies only", StringComparison.Ordinal);
        depsOnlyIndex.ShouldBeGreaterThan(installIndex);

        var installStep = tddJob.Substring(installIndex, depsOnlyIndex - installIndex);
        installStep.ShouldContain("cache-hit != 'true'");
        installStep.ShouldContain("Remove-Item -Recurse -Force $playwrightCache");
        installStep.ShouldContain(".cache/ms-playwright");
    }

    private static string ReadTddJob(string yaml)
    {
        var tddStart = yaml.IndexOf("  deploy-to-tdd:", StringComparison.Ordinal);
        tddStart.ShouldBeGreaterThan(-1);
        var uatStart = yaml.IndexOf("  deploy-to-uat:", tddStart, StringComparison.Ordinal);
        uatStart.ShouldBeGreaterThan(tddStart);
        return yaml.Substring(tddStart, uatStart - tddStart);
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
