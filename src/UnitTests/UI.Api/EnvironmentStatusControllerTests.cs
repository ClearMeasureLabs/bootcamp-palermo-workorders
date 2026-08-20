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
    [Test]
    public void Get_Should_ReturnJson_WithOsProcessorClrAndEnvVars_When_Called()
    {
        var controller = new EnvironmentStatusController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
        payload.OsDescription.ShouldNotBeNullOrWhiteSpace();
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ProcessorCount.ShouldBeGreaterThanOrEqualTo(1);
        payload.ClrVersion.ShouldBe(RuntimeInformation.FrameworkDescription);
        payload.ClrVersion.ShouldNotBeNullOrWhiteSpace();
        payload.EnvironmentVariables.ShouldNotBeNull();
    }

    [Test]
    public void Get_Should_RedactEnvironmentVariableValues_When_AllowlistedVarsPresent()
    {
        var previousAspNet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var previousDbEngine = Environment.GetEnvironmentVariable("DATABASE_ENGINE");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "SecretStagingValue-xyz");
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", "SecretEngineValue-abc");
        try
        {
            var controller = new EnvironmentStatusController
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = controller.Get();

            var content = result.ShouldBeOfType<ContentResult>();
            var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
                content.Content!,
                ConditionalGetEtag.JsonSerializerOptions);
            payload.ShouldNotBeNull();
            payload!.EnvironmentVariables.ContainsKey("ASPNETCORE_ENVIRONMENT").ShouldBeTrue();
            payload.EnvironmentVariables.ContainsKey("DATABASE_ENGINE").ShouldBeTrue();
            payload.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"].ShouldBe(EnvironmentStatusSnapshot.RedactedValue);
            payload.EnvironmentVariables["DATABASE_ENGINE"].ShouldBe(EnvironmentStatusSnapshot.RedactedValue);
            var json = content.Content.ShouldNotBeNull();
            json.ShouldNotContain("SecretStagingValue-xyz");
            json.ShouldNotContain("SecretEngineValue-abc");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousAspNet);
            Environment.SetEnvironmentVariable("DATABASE_ENGINE", previousDbEngine);
        }
    }

    [Test]
    public void Get_Should_OmitMissingAllowlistedNames_When_EnvVarAbsent()
    {
        var snapshot = EnvironmentStatusSnapshot.Build(_ => null);

        snapshot.EnvironmentVariables.Count.ShouldBe(0);
        snapshot.EnvironmentVariables.ContainsKey("ASPNETCORE_ENVIRONMENT").ShouldBeFalse();
        snapshot.EnvironmentVariables.ContainsKey("DATABASE_ENGINE").ShouldBeFalse();
    }
}
