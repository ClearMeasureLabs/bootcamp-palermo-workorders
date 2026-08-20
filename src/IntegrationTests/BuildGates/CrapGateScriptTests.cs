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

    [Test]
    public void FlattenCobertura_WhenAsyncStateMachine_CopiesHitsOntoOriginalMethod()
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, ".cursor", "skills", "crap-score-cleanup", "scripts",
            "flatten-cobertura.csx");
        var fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures",
            "cobertura-async-state-machine.xml");
        var output = Path.Combine(Path.GetTempPath(), $"crap-flat-{Guid.NewGuid():N}.xml");
        File.Exists(script).ShouldBeTrue(script);
        File.Exists(fixture).ShouldBeTrue(fixture);

        var logs = RunDotnetScript(script, output, fixture, out var exitCode);
        exitCode.ShouldBe(0, logs);

        var xml = File.ReadAllText(output);
        xml.ShouldContain("name=\"ExecuteCommandAsync\"");
        xml.ShouldContain("number=\"90\"");
        File.Delete(output);
    }

    [Test]
    public void FlattenCobertura_WhenOnlyStateMachineClassExists_SynthesizesParentMethod()
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, ".cursor", "skills", "crap-score-cleanup", "scripts",
            "flatten-cobertura.csx");
        var fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures",
            "cobertura-orphan-async-state-machine.xml");
        var output = Path.Combine(Path.GetTempPath(), $"crap-orphan-{Guid.NewGuid():N}.xml");
        File.Exists(fixture).ShouldBeTrue(fixture);

        var logs = RunDotnetScript(script, output, fixture, out var exitCode);
        exitCode.ShouldBe(0, logs);

        var xml = File.ReadAllText(output);
        xml.ShouldContain("name=\"ClearMeasure.Bootcamp.UI.Server.RequestBodyBufferingPipeline\"");
        xml.ShouldContain("name=\"InvokeAsync\"");
        File.Delete(output);
    }

    private static string RunDotnetScript(string script, string output, string fixture, out int exitCode)
    {
        var tool = EnsureDotnetScript();
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = tool,
            ArgumentList = { script, "--", output, fixture },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        process.ShouldNotBeNull();
        process!.WaitForExit(60_000).ShouldBeTrue();
        exitCode = process.ExitCode;
        return process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
    }

    private static string EnsureDotnetScript()
    {
        var fileName = OperatingSystem.IsWindows() ? "dotnet-script.exe" : "dotnet-script";
        var installed = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet", "tools", fileName);
        if (File.Exists(installed))
        {
            return installed;
        }

        var existing = FindOnPath(fileName);
        if (existing != null)
        {
            return existing;
        }

        using var install = Process.Start(new ProcessStartInfo)
        {
            FileName = "dotnet",
            ArgumentList = { "tool", "install", "-g", "dotnet-script", "--version", "1.6.0" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        install.ShouldNotBeNull();
        install!.WaitForExit(120_000).ShouldBeTrue();
        var logs = install.StandardError.ReadToEnd() + install.StandardOutput.ReadToEnd();
        if (File.Exists(installed))
        {
            return installed;
        }

        var pathLookup = FindOnPath(fileName);
        pathLookup.ShouldNotBeNull($"dotnet-script not found after install. {logs}");
        return pathLookup!;
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
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
