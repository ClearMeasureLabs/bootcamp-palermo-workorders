using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusBuilderTests
{
    private const string VarA = "CB_ENV_BUILDER_VAR_A_8355";
    private const string VarB = "CB_ENV_BUILDER_VAR_B_8355";
    private const string UnlistedVar = "CB_ENV_BUILDER_UNLISTED_8355";

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Environment.SetEnvironmentVariable(VarA, null);
        Environment.SetEnvironmentVariable(VarB, null);
        Environment.SetEnvironmentVariable(UnlistedVar, null);
    }

    [Test]
    public void Build_Should_ReadOsDescriptionFromRuntime()
    {
        var result = EnvironmentStatusBuilder.Build(Options.Create(new EnvironmentDiagnosticsOptions()));

        result.OsDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
    }

    [Test]
    public void Build_Should_ReadProcessorCountFromEnvironment()
    {
        var result = EnvironmentStatusBuilder.Build(Options.Create(new EnvironmentDiagnosticsOptions()));

        result.ProcessorCount.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public void Build_Should_ReadClrVersionFromRuntime()
    {
        var result = EnvironmentStatusBuilder.Build(Options.Create(new EnvironmentDiagnosticsOptions()));

        result.ClrVersion.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void Build_Should_IncludeAllowlistedVariablesAsRedactedPairs_When_AllowlistIsConfigured()
    {
        Environment.SetEnvironmentVariable(VarA, "value-a");
        Environment.SetEnvironmentVariable(VarB, "value-b");
        var options = Options.Create(new EnvironmentDiagnosticsOptions
        {
            VariableNames = [VarA, VarB]
        });

        var result = EnvironmentStatusBuilder.Build(options);

        result.EnvironmentVariables.Count.ShouldBe(2);
        result.EnvironmentVariables[VarA].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        result.EnvironmentVariables[VarB].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
    }

    [Test]
    public void Build_Should_OmitVariableKeysNotInAllowlist_When_AllowlistIsConfigured()
    {
        Environment.SetEnvironmentVariable(UnlistedVar, "secret");
        Environment.SetEnvironmentVariable(VarA, null);
        var options = Options.Create(new EnvironmentDiagnosticsOptions
        {
            VariableNames = [VarA]
        });

        var result = EnvironmentStatusBuilder.Build(options);

        result.EnvironmentVariables.ShouldNotContainKey(UnlistedVar);
        result.EnvironmentVariables.ShouldNotContainKey(VarA);
    }

    [Test]
    public void Build_Should_ReturnEmptyVariablesDictionary_When_AllowlistIsEmpty()
    {
        Environment.SetEnvironmentVariable(UnlistedVar, "secret");
        var options = Options.Create(new EnvironmentDiagnosticsOptions { VariableNames = [] });

        var result = EnvironmentStatusBuilder.Build(options);

        result.EnvironmentVariables.ShouldBeEmpty();
    }
}
