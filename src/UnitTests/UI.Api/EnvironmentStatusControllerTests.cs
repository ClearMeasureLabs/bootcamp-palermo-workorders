using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusControllerTests
{
    private const string DistinctiveSecretEnvName = "BOOTCAMP_ENV_STATUS_TEST_SECRET_6191";
    private const string DistinctiveSecretValue = "distinctive-secret-value-6191-never-in-json";

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(DistinctiveSecretEnvName, null);
    }

    [Test]
    public void Get_Should_ReturnJson_WithOsProcessorClrFrameworkAndRedactedEnvVars_When_Called()
    {
        var opts = Options.Create(new RuntimeEnvironmentStatusOptions
        {
            VariableNames = ["ASPNETCORE_ENVIRONMENT", "HOSTNAME"]
        });
        var controller = new EnvironmentStatusController(opts)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<RuntimeEnvironmentResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBeGreaterThanOrEqualTo(0);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        payload.EnvironmentVariables.Count.ShouldBe(2);
        payload.EnvironmentVariables[0].Name.ShouldBe("ASPNETCORE_ENVIRONMENT");
        payload.EnvironmentVariables[0].ValueRedacted.ShouldBeTrue();
        payload.EnvironmentVariables[1].Name.ShouldBe("HOSTNAME");
        payload.EnvironmentVariables[1].ValueRedacted.ShouldBeTrue();
    }

    [Test]
    public void Get_Should_NeverIncludeRawEnvValue_When_AllowlistedVarHasDistinctiveValue()
    {
        Environment.SetEnvironmentVariable(DistinctiveSecretEnvName, DistinctiveSecretValue);
        var opts = Options.Create(new RuntimeEnvironmentStatusOptions
        {
            VariableNames = [DistinctiveSecretEnvName]
        });
        var controller = new EnvironmentStatusController(opts)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();
        var content = result.ShouldBeOfType<ContentResult>();

        content.Content!.ShouldNotContain(DistinctiveSecretValue);
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesWeakEtag()
    {
        var opts = Options.Create(new RuntimeEnvironmentStatusOptions { VariableNames = [] });
        var controller = new EnvironmentStatusController(opts)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        _ = controller.Get();
        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrWhiteSpace();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag;
        var controller2 = new EnvironmentStatusController(opts)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var second = controller2.Get();
        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public void ResolveAllowlistedNames_Should_CapAndDedupe_When_ConfigHasDuplicatesAndOverflow()
    {
        var many = Enumerable
            .Range(0, RuntimeEnvironmentStatusOptions.MaxVariableNames + 10)
            .Select(i => $"VAR_{i}")
            .Append("VAR_0")
            .ToArray();
        var list = EnvironmentStatusController.ResolveAllowlistedNames(new RuntimeEnvironmentStatusOptions
        {
            VariableNames = many
        });
        list.Count.ShouldBe(RuntimeEnvironmentStatusOptions.MaxVariableNames);
        list.Distinct(StringComparer.Ordinal).Count().ShouldBe(list.Count);
    }

    [Test]
    public void ResolveAllowlistedNames_Should_UseDefaults_When_VariableNamesEmpty()
    {
        var list = EnvironmentStatusController.ResolveAllowlistedNames(new RuntimeEnvironmentStatusOptions
        {
            VariableNames = []
        });
        list.ShouldBe(RuntimeEnvironmentStatusOptions.DefaultVariableNames.ToList());
    }
}
