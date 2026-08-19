using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusBuilderTests
{
    [Test]
    public void Build_Should_RedactValues_When_MonitoredVariablesAreSet()
    {
        const string secretValue = "super-secret-connection-string";
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", secretValue);
        Environment.SetEnvironmentVariable("AI_OpenAI_ApiKey", "sk-live-key");
        try
        {
            var payload = EnvironmentStatusBuilder.Build(new StubHostEnvironment("Testing"));

            payload.EnvironmentVariables.ContainsKey("ConnectionStrings__SqlConnectionString").ShouldBeTrue();
            payload.EnvironmentVariables["ConnectionStrings__SqlConnectionString"].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
            payload.EnvironmentVariables.ContainsKey("AI_OpenAI_ApiKey").ShouldBeTrue();
            payload.EnvironmentVariables["AI_OpenAI_ApiKey"].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
            payload.EnvironmentVariables.Values.ShouldNotContain(secretValue);
            payload.EnvironmentVariables.Values.ShouldNotContain("sk-live-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", null);
            Environment.SetEnvironmentVariable("AI_OpenAI_ApiKey", null);
        }
    }

    [Test]
    public void Build_Should_OmitUnsetVariables_When_NotPresentInProcessEnvironment()
    {
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", null);
        try
        {
            var payload = EnvironmentStatusBuilder.Build(new StubHostEnvironment("Testing"));

            payload.EnvironmentVariables.ContainsKey("DATABASE_ENGINE").ShouldBeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_ENGINE", null);
        }
    }

    [Test]
    public void Build_Should_UseConfiguredMonitoredVariables_When_OptionsProvided()
    {
        const string customName = "ENV_STATUS_BUILDER_TEST_VAR";
        Environment.SetEnvironmentVariable(customName, "visible-if-not-redacted");
        try
        {
            var options = new EnvironmentStatusOptions
            {
                MonitoredVariables = [customName]
            };
            var payload = EnvironmentStatusBuilder.Build(new StubHostEnvironment("Testing"), options);

            payload.EnvironmentVariables.Keys.ShouldBe([customName]);
            payload.EnvironmentVariables[customName].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(customName, null);
        }
    }

    [Test]
    public void Build_Should_PopulateRuntimeFields_FromBcl()
    {
        var payload = EnvironmentStatusBuilder.Build(new StubHostEnvironment("Staging"));

        payload.OsDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.HostEnvironmentName.ShouldBe("Staging");
    }

    private sealed class StubHostEnvironment(string environmentName) : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
