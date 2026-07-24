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
        payload.BuildVersion.ShouldNotBeNullOrEmpty();
        payload.BuildVersion.ShouldBe(payload.AssemblyVersion);
        payload.Environment.ShouldBe("TestEnvironment");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void Get_Should_ReturnOk_WithBuildVersionAndCommitHashFields()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var controller = new VersionController(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var document = JsonDocument.Parse(content.Content!);
        var root = document.RootElement;
        root.TryGetProperty("buildVersion", out _).ShouldBeTrue();
        root.TryGetProperty("commitHash", out var commitHash).ShouldBeTrue();
        commitHash.ValueKind.ShouldBeOneOf(JsonValueKind.String, JsonValueKind.Null);
    }

    [Test]
    public void Get_Should_ReturnNullCommitHash_When_NoGitMetadata()
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
        payload!.BuildVersion.ShouldNotBeNullOrEmpty();
        if (payload.InformationalVersion?.Contains('+') != true)
            payload.CommitHash.ShouldBeNull();
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
