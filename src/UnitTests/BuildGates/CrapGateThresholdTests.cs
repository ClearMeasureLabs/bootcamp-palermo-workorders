using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapGateThresholdTests
{
    [Test]
    public void ReadProductionThreshold_WhenConfigExists_ReturnsPositiveInteger()
    {
        var threshold = CrapGateThreshold.ReadProductionThreshold();

        threshold.ShouldBeGreaterThan(0);
    }

    [Test]
    public void EnforcementEntrypoints_WhenRead_DoNotHardcodeGateThresholdLiterals()
    {
        var threshold = CrapGateThreshold.ReadProductionThreshold();
        var privateBuild = File.ReadAllText(FindRepoFile("PrivateBuild.ps1"));
        var workflow = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));
        var auditScript = File.ReadAllText(FindRepoFile(Path.Combine(
            ".cursor", "skills", "crap-score-cleanup", "scripts", "run-crap-audit.ps1")));
        var skill = File.ReadAllText(FindRepoFile(Path.Combine(
            ".cursor", "skills", "crap-score-cleanup", "SKILL.md")));
        var config = File.ReadAllText(FindRepoFile(CrapGateThreshold.RelativeConfigPath));

        config.ShouldContain($"\"productionThreshold\": {threshold}");
        privateBuild.ShouldContain("crap-gate-threshold.json");
        privateBuild.ShouldNotContain($"-Threshold {threshold}");
        workflow.ShouldContain("crap-gate-threshold.json");
        workflow.ShouldNotContain($"-Threshold {threshold}");
        workflow.ShouldContain("name: Enforce CRAP (production)");
        auditScript.ShouldContain("crap-gate-threshold.json");
        auditScript.ShouldContain("Get-ProductionCrapGateThreshold");
        auditScript.ShouldNotContain($"[int]$Threshold = {threshold}");
        skill.ShouldContain("crap-gate-threshold.json");
        skill.ShouldContain("productionThreshold");
        skill.ShouldNotContain($"-Threshold {threshold}");
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
