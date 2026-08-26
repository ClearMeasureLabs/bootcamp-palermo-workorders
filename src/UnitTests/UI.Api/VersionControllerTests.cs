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
        var controller = CreateController(stubHostEnvironment);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.InformationalVersion.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.Environment.ShouldBe("TestEnvironment");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void Get_Should_IncludeBuildConfiguration_MatchingAssemblyAttribute()
    {
        var apiAssembly = typeof(VersionController).Assembly;
        var expected = apiAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        if (string.IsNullOrWhiteSpace(expected))
        {
#if DEBUG
            expected = "Debug";
#else
            expected = "Release";
#endif
        }

        var controller = CreateController(new StubHostEnvironment("Test"));
        var result = controller.Get();
        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);

        payload.ShouldNotBeNull();
        payload.BuildConfiguration.ShouldBe(expected);
        payload.BuildConfiguration.ShouldBeOneOf("Debug", "Release");
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesPayloadEtag()
    {
        var controller = CreateController(new StubHostEnvironment("Test"));
        var first = controller.Get();
        first.ShouldBeOfType<ContentResult>();
        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        controller.Request.Headers.IfNoneMatch = etag;
        var second = controller.Get();

        var status = second.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private static VersionController CreateController(IHostEnvironment hostEnvironment)
    {
        return new VersionController(hostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
