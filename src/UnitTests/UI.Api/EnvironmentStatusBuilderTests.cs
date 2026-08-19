using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusBuilderTests
{
    [Test]
    public void BuildRedactedEnvironmentVariables_Should_OmitUnsetNames_When_NotPresent()
    {
        var monitored = new[] { "8457_UNSET_A", "8457_UNSET_B" };

        var result = EnvironmentStatusBuilder.BuildRedactedEnvironmentVariables(monitored);

        result.Count.ShouldBe(0);
    }

    [Test]
    public void BuildRedactedEnvironmentVariables_Should_RedactValues_When_VariablesAreSet()
    {
        const string varName = "8457_TEST_REDACT";
        const string secret = "super-secret-value-8457";
        Environment.SetEnvironmentVariable(varName, secret);
        try
        {
            var result = EnvironmentStatusBuilder.BuildRedactedEnvironmentVariables([varName]);

            result.Count.ShouldBe(1);
            result.ShouldContainKey(varName);
            result[varName].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
            result[varName].ShouldNotBe(secret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
    }

    [Test]
    public void BuildRedactedEnvironmentVariables_Should_NeverEmitRawValues_When_MultipleVariablesSet()
    {
        const string varA = "8457_TEST_RAW_A";
        const string varB = "8457_TEST_RAW_B";
        const string valueA = "raw-value-a-8457";
        const string valueB = "raw-value-b-8457";
        Environment.SetEnvironmentVariable(varA, valueA);
        Environment.SetEnvironmentVariable(varB, valueB);
        try
        {
            var result = EnvironmentStatusBuilder.BuildRedactedEnvironmentVariables([varA, varB]);
            var serialized = string.Join('|', result.Select(kv => $"{kv.Key}={kv.Value}"));

            serialized.ShouldNotContain(valueA);
            serialized.ShouldNotContain(valueB);
            result[varA].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
            result[varB].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varA, null);
            Environment.SetEnvironmentVariable(varB, null);
        }
    }

    [Test]
    public void CollectMonitoredVariableNames_Should_IncludeDefaults_When_OptionsNull()
    {
        var names = EnvironmentStatusBuilder.CollectMonitoredVariableNames(null);

        names.ShouldContain("ASPNETCORE_ENVIRONMENT");
        names.ShouldContain("DATABASE_ENGINE");
        names.ShouldContain("ConnectionStrings__SqlConnectionString");
        names.ShouldContain("AI_OpenAI_ApiKey");
    }

    [Test]
    public void CollectMonitoredVariableNames_Should_MergeConfiguredExtras_When_OptionsProvided()
    {
        var options = new EnvironmentStatusOptions
        {
            MonitoredVariables = ["CUSTOM_VAR_8457", "ASPNETCORE_ENVIRONMENT"]
        };

        var names = EnvironmentStatusBuilder.CollectMonitoredVariableNames(options);

        names.ShouldContain("CUSTOM_VAR_8457");
        names.Count(n => n == "ASPNETCORE_ENVIRONMENT").ShouldBe(1);
    }

    [Test]
    public void Build_Should_RedactConfiguredVariables_When_VariablesAreSet()
    {
        const string varName = "8457_BUILD_REDACT";
        const string secret = "build-secret-8457";
        Environment.SetEnvironmentVariable(varName, secret);
        try
        {
            var options = new EnvironmentStatusOptions { MonitoredVariables = [varName] };
            var stubHost = new StubHostEnvironment("BuildTest8457");
            var response = EnvironmentStatusBuilder.Build(stubHost, options);

            response.EnvironmentVariables.ShouldContainKey(varName);
            response.EnvironmentVariables[varName].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
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
