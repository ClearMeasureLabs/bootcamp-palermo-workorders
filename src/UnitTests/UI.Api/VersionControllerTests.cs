using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class VersionControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithExpectedShape()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.InformationalVersion.ShouldNotBeNullOrEmpty();
        payload.Environment.ShouldBe("TestEnvironment");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void Get_Should_IncludeBuildConfiguration()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.BuildConfiguration.ShouldNotBeNullOrEmpty();

        var expected = typeof(VersionController).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        payload.BuildConfiguration.ShouldBe(expected);
    }

    [Test]
    public void Get_Should_ReturnWeakEtag()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        etag.IsWeak.ShouldBeTrue();
        etag.Tag.ToString().ShouldStartWith("\"");
        etag.Tag.ToString().ShouldEndWith("\"");

        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
        EntityTagHeaderValue.TryParse(controller.Response.Headers.ETag.ToString(), out var parsed).ShouldBeTrue();
        parsed!.IsWeak.ShouldBeTrue();
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatches()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var httpContext = new DefaultHttpContext();
        var controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var first = controller.Get();
        first.ShouldBeOfType<ContentResult>();
        var etagValue = controller.Response.Headers.ETag.ToString();
        etagValue.ShouldNotBeNullOrEmpty();

        httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etagValue;
        controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var second = controller.Get();
        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
