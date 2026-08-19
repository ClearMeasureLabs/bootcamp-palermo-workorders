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
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.Environment.ShouldBe("TestEnvironment");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    }

    [Test]
    public void Get_Should_ReturnBuildConfiguration_WhenPresent()
    {
        var expectedConfiguration = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!
            .Configuration;
        var controller = new VersionController(new StubHostEnvironment("Development"))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var content = controller.Get().ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);

        payload.ShouldNotBeNull();
        payload!.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldBe(expectedConfiguration);
    }

    [Test]
    public void Get_Should_ReturnValidJsonShape_WithAllRequiredFields()
    {
        var controller = new VersionController(new StubHostEnvironment("Staging"))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var content = controller.Get().ShouldBeOfType<ContentResult>();
        using var document = JsonDocument.Parse(content.Content!);
        var root = document.RootElement;

        root.GetProperty("assemblyVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("informationalVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("buildConfiguration").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("environment").GetString().ShouldBe("Staging");
        root.GetProperty("machineName").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_ReturnExpectedEnvironment_ViaIHostEnvironment()
    {
        var controller = new VersionController(new StubHostEnvironment("Production"))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var content = controller.Get().ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);

        payload.ShouldNotBeNull();
        payload!.Environment.ShouldBe("Production");
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
