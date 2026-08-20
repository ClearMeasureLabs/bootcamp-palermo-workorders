using System.Diagnostics;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

[TestFixture]
public class CrapGateScriptTests
{
    [Test]
    public void AssertCrapGate_WhenNoProductionViolations_ExitsZero()
    {
        var exitCode = RunAssertScript("crap-production-violations-pass.json");

        exitCode.ShouldBe(0);
    }

    [Test]
    public void AssertCrapGate_WhenProductionMethodOverThreshold_ExitsOne()
    {
        var exitCode = RunAssertScript("crap-production-violations-fail.json");

        exitCode.ShouldBe(1);
    }

    private static int RunAssertScript(string fixtureFileName)
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, ".cursor", "skills", "crap-score-cleanup", "scripts",
            "assert-crap-gate.ps1");
        var fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures",
            fixtureFileName);
        File.Exists(script).ShouldBeTrue(script);
        File.Exists(fixture).ShouldBeTrue(fixture);

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pwsh",
            ArgumentList = { "-NoProfile", "-File", script, "-ViolationsPath", fixture },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        process.ShouldNotBeNull();
        process!.WaitForExit(30_000).ShouldBeTrue();
        return process.ExitCode;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}
