using System.Runtime.InteropServices;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusResponseBuilderTests
{
    private static readonly string[] CuratedEnvironmentVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE",
        "ConnectionStrings__SqlConnectionString",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "AI_OpenAI_ApiKey",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model",
        "ApiKeyAuthentication__Enabled",
        "ApiKeyAuthentication__ValidationKey",
        "OTEL_EXPORTER_OTLP_ENDPOINT"
    ];

    [Test]
    public void Should_ReturnOsProcessorAndClrMetadata_When_BuildCalled()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var response = EnvironmentStatusResponseBuilder.Build(configuration);

        response.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
        response.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        response.ClrVersion.ShouldBe(Environment.Version.ToString());
    }

    [Test]
    public void Should_RedactAllEnvironmentVariableValues_When_VariablesAreSet()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["DATABASE_ENGINE"] = "SqlServer",
                ["ConnectionStrings:SqlConnectionString"] = "Server=secret;",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=abc",
                ["AI_OpenAI_ApiKey"] = "super-secret-key",
                ["AI_OpenAI_Url"] = "https://secret.openai.azure.com",
                ["AI_OpenAI_Model"] = "gpt-secret",
                ["ApiKeyAuthentication:Enabled"] = "true",
                ["ApiKeyAuthentication:ValidationKey"] = "validation-secret",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://otel-secret.example"
            })
            .Build();

        var response = EnvironmentStatusResponseBuilder.Build(configuration);

        foreach (var value in response.EnvironmentVariables.Values)
        {
            value.ShouldBe(EnvironmentStatusResponseBuilder.RedactedEnvironmentVariableValue);
        }

        response.EnvironmentVariables.Values.ShouldNotContain("Production");
        response.EnvironmentVariables.Values.ShouldNotContain("SqlServer");
        response.EnvironmentVariables.Values.ShouldNotContain("Server=secret;");
        response.EnvironmentVariables.Values.ShouldNotContain("super-secret-key");
        response.EnvironmentVariables.Values.ShouldNotContain("validation-secret");
    }

    [Test]
    public void Should_IncludeCuratedVariableNames_When_VariablesUnset()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var response = EnvironmentStatusResponseBuilder.Build(configuration);

        response.EnvironmentVariables.Count.ShouldBe(CuratedEnvironmentVariableNames.Length);
        foreach (var name in CuratedEnvironmentVariableNames)
        {
            response.EnvironmentVariables.ContainsKey(name).ShouldBeTrue();
            response.EnvironmentVariables[name].ShouldBe(EnvironmentStatusResponseBuilder.RedactedEnvironmentVariableValue);
        }
    }

    [Test]
    public void Should_PreferConfigurationOverEnvironment_When_BothPresent()
    {
        const string envVarName = "ConnectionStrings__SqlConnectionString";
        const string configurationValue = "Server=config-secret;";
        const string environmentValue = "Server=env-secret;";
        Environment.SetEnvironmentVariable(envVarName, environmentValue);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SqlConnectionString"] = configurationValue
                })
                .Build();

            var response = EnvironmentStatusResponseBuilder.Build(configuration);

            response.EnvironmentVariables.ShouldContainKey(envVarName);
            response.EnvironmentVariables[envVarName]
                .ShouldBe(EnvironmentStatusResponseBuilder.RedactedEnvironmentVariableValue);
            response.EnvironmentVariables.Values.ShouldNotContain(configurationValue);
            response.EnvironmentVariables.Values.ShouldNotContain(environmentValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }
}
