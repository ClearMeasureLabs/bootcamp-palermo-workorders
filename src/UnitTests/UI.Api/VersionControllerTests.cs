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
        var expectedConfiguration = typeof(VersionController).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        payload.BuildConfiguration.ShouldBe(expectedConfiguration);
        payload.Environment.ShouldBe("TestEnvironment");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var firstController = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        firstController.Get();
        var etag = firstController.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        var secondContext = new DefaultHttpContext();
        secondContext.Request.Headers.IfNoneMatch = etag;
        var secondController = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = secondContext }
        };

        var result = secondController.Get();

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public void Get_Should_Return200WithBody_When_IfNoneMatchMisses()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var context = new DefaultHttpContext();
        context.Request.Headers.IfNoneMatch = "W/\"not-the-real-etag\"";
        var controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.Content.ShouldNotBeNullOrEmpty();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldBe(
            typeof(VersionController).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
