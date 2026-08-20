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
    public void Get_Should_Return200WithValidJsonShape()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var controller = CreateController(stubHostEnvironment);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = DeserializePayload(content.Content!);
        payload.AssemblyVersion.ShouldNotBeNull();
        payload.InformationalVersion.ShouldNotBeNull();
        payload.BuildConfiguration.ShouldNotBeNull();
        payload.Environment.ShouldNotBeNull();
    }

    [Test]
    public void Get_Should_HaveNonEmptyVersionFields()
    {
        var controller = CreateController(new StubHostEnvironment("TestEnvironment"));

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializePayload(content.Content!);
        payload.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.InformationalVersion.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_IncludeBuildConfiguration()
    {
        var controller = CreateController(new StubHostEnvironment("TestEnvironment"));

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializePayload(content.Content!);
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_IncludeEnvironmentName()
    {
        var controller = CreateController(new StubHostEnvironment("Staging"));

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializePayload(content.Content!);
        payload.Environment.ShouldBe("Staging");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    private static VersionController CreateController(StubHostEnvironment stubHostEnvironment) =>
        new(stubHostEnvironment)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static VersionMetadataResponse DeserializePayload(string json) =>
        JsonSerializer.Deserialize<VersionMetadataResponse>(
            json,
            ConditionalGetEtag.JsonSerializerOptions).ShouldNotBeNull();

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
