using System.Runtime.InteropServices;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusControllerTests
{
    private const string SecretConnectionString = "Server=secret;Password=not-for-json";
    private const string SecretOpenAiKey = "sk-test-secret-key";

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", null);
        Environment.SetEnvironmentVariable("AI_OpenAI_ApiKey", null);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
    }

    [Test]
    public void GetEnvironment_Should_ReturnJson_WithOsProcessorAndClrFields_When_Called()
    {
        var controller = CreateController();

        var result = controller.GetEnvironment();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
    }

    [Test]
    public void GetEnvironment_Should_RedactAllowlistedEnvVarValues_When_SecretsSet()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", SecretConnectionString);
        Environment.SetEnvironmentVariable("AI_OpenAI_ApiKey", SecretOpenAiKey);
        var controller = CreateController();

        var result = controller.GetEnvironment();

        var content = result.ShouldBeOfType<ContentResult>();
        var json = content.Content!;
        json.ShouldNotContain(SecretConnectionString);
        json.ShouldNotContain(SecretOpenAiKey);
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            json,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldContain(e =>
            e.Name == "ConnectionStrings__SqlConnectionString"
            && e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue);
        payload.EnvironmentVariables.ShouldContain(e =>
            e.Name == "AI_OpenAI_ApiKey"
            && e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue);
    }

    [Test]
    public void GetEnvironment_Should_IncludeOnlyAllowlistedNames_When_EnvVarsPresent()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", SecretConnectionString);
        Environment.SetEnvironmentVariable("EnvironmentStatusControllerTests_Extra", "extra");
        var controller = CreateController();

        var result = controller.GetEnvironment();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.All(e => e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue).ShouldBeTrue();
        payload.EnvironmentVariables.Any(e => e.Name == "EnvironmentStatusControllerTests_Extra").ShouldBeFalse();
    }

    [Test]
    public void GetEnvironment_Should_OmitUnsetAllowlistKeys_When_NotDefined()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        var controller = CreateController();

        var result = controller.GetEnvironment();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.Any(e => e.Name == "OTEL_EXPORTER_OTLP_ENDPOINT").ShouldBeFalse();
    }

    private static StatusController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
