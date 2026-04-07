using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusBuilderTests
{
    [Test]
    public void Build_Should_RedactAllListedVariableValues()
    {
        var stubHost = new StubHostEnvironment("UnitTest");
        var config = new ConfigurationBuilder().Build();

        var payload = EnvironmentStatusBuilder.Build(stubHost, config);

        payload.EnvironmentVariables.Count.ShouldBe(EnvironmentStatusBuilder.ReportedVariableNames.Count);
        foreach (var entry in payload.EnvironmentVariables)
        {
            entry.Value.ShouldBe(EnvironmentStatusBuilder.RedactedValueMarker);
            EnvironmentStatusBuilder.ReportedVariableNames.ShouldContain(entry.Name);
        }
    }

    [Test]
    public void Build_Should_IncludeRuntimeFields_MatchingHost()
    {
        var stubHost = new StubHostEnvironment("StagingCheck");
        var config = new ConfigurationBuilder().Build();

        var payload = EnvironmentStatusBuilder.Build(stubHost, config);

        payload.OsDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        payload.HostEnvironmentName.ShouldBe("StagingCheck");
    }

    [Test]
    public void Build_Should_MarkVariableSet_When_SimulatedViaConfiguration()
    {
        var stubHost = new StubHostEnvironment("UnitTest");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [EnvironmentStatusBuilder.SimulatedEnvironmentVariablesConfigurationPrefix + "ENV_STATUS_PROBE_SECRET"] = "configured-but-never-serialized"
        }).Build();

        var payload = EnvironmentStatusBuilder.Build(stubHost, config);
        var probe = payload.EnvironmentVariables.Single(e => e.Name == "ENV_STATUS_PROBE_SECRET");

        probe.IsSet.ShouldBeTrue();
        probe.Value.ShouldBe(EnvironmentStatusBuilder.RedactedValueMarker);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
