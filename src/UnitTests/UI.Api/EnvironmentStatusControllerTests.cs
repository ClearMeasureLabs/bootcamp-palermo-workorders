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
    private const string ProbeVariableName = "ENV_STATUS_UNIT_TEST_PROBE_6273";

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(ProbeVariableName, null);
    }

    [Test]
    public void Get_Should_ReturnOk_WithExpectedShape()
    {
        var options = Options.Create(new EnvironmentStatusOptions
        {
            IncludedEnvironmentVariables = ["ASPNETCORE_ENVIRONMENT"]
        });
        var controller = new EnvironmentStatusController(options)
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
        payload!.OsDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.FrameworkDescription.ShouldBe(
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        payload.EnvironmentVariables.Count.ShouldBe(1);
        payload.EnvironmentVariables[0].Name.ShouldBe("ASPNETCORE_ENVIRONMENT");
    }

    [Test]
    public void Get_Should_NeverExposeVariableValues_When_VariableIsSet()
    {
        Environment.SetEnvironmentVariable(ProbeVariableName, "classified-secret-value-6273");
        var options = Options.Create(new EnvironmentStatusOptions
        {
            IncludedEnvironmentVariables = [ProbeVariableName]
        });
        var controller = new EnvironmentStatusController(options)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = (result as ContentResult)?.Content;
        content.ShouldNotBeNull();
        content!.ShouldNotContain("classified-secret");
        content.ShouldNotContain("secret");
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content,
            ConditionalGetEtag.JsonSerializerOptions);
        payload!.EnvironmentVariables[0].IsSet.ShouldBeTrue();
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var options = Options.Create(new EnvironmentStatusOptions
        {
            IncludedEnvironmentVariables = []
        });
        var controller = new EnvironmentStatusController(options)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var first = controller.Get();
        var firstContent = first.ShouldBeOfType<ContentResult>();
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(
            JsonSerializer.Deserialize<EnvironmentStatusResponse>(
                firstContent.Content!,
                ConditionalGetEtag.JsonSerializerOptions)!);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag.ToString();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var second = controller.Get();
        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }
}
