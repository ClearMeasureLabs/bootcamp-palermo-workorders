using System.Diagnostics;
using System.Xml.Linq;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CompilerWarningGateTests
{
    [Test]
    public void ShouldConfigurePrivateBuildCompile_WhenBuildScriptIsRead()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "build.ps1"));
        var compileStart = source.IndexOf("Function Compile", StringComparison.Ordinal);
        var compileEnd = source.IndexOf("Function UnitTests", compileStart, StringComparison.Ordinal);

        compileStart.ShouldBeGreaterThan(-1);
        compileEnd.ShouldBeGreaterThan(compileStart);
        var compile = source[compileStart..compileEnd];
        compile.ShouldContain("/p:TreatWarningsAsErrors=\"true\"");
        compile.ShouldContain("/p:MSBuildTreatAllWarningsAsErrors=\"true\"");
    }

    [Test]
    public void ShouldKeepOrdinaryBuildsTolerant_WhenProjectDefaultsAreRead()
    {
        var repoRoot = FindRepoRoot();
        var projectDefaults = Directory
            .EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(repoRoot, "Directory.Build.props", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(repoRoot, "Directory.Build.targets", SearchOption.AllDirectories));

        var strictDefaults = projectDefaults
            .SelectMany(path => XDocument.Load(path)
                .Descendants()
                .Where(element =>
                    element.Name.LocalName is
                        "TreatWarningsAsErrors" or "MSBuildTreatAllWarningsAsErrors"
                    && bool.TryParse(element.Value, out var enabled)
                    && enabled)
                .Select(_ => path))
            .ToArray();

        strictDefaults.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldFailOnlyStrictBuild_WhenCompilerWarningIsPresent()
    {
        var projectDirectory = Path.Combine(
            Path.GetTempPath(),
            $"compiler-warning-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "WarningGate.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "Program.cs"),
                """
                #warning Warning gate contract
                System.Console.WriteLine("warning gate");
                """);

            var ordinaryBuild = await RunDotNetBuild(projectDirectory);
            var strictBuild = await RunDotNetBuild(
                projectDirectory,
                "/p:TreatWarningsAsErrors=true",
                "/p:MSBuildTreatAllWarningsAsErrors=true");

            ordinaryBuild.ExitCode.ShouldBe(0, ordinaryBuild.Output);
            ordinaryBuild.Output.ShouldContain("warning CS1030");
            strictBuild.ExitCode.ShouldNotBe(0, strictBuild.Output);
            strictBuild.Output.ShouldContain("error CS1030");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    private static async Task<BuildResult> RunDotNetBuild(
        string workingDirectory,
        params string[] additionalArguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--no-incremental");
        foreach (var argument in additionalArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet build.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new BuildResult(
            process.ExitCode,
            $"{await standardOutput}{Environment.NewLine}{await standardError}");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "build.ps1")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found from test directory.");
    }

    private sealed record BuildResult(int ExitCode, string Output);
}
