using System.Reflection;
using System.Reflection.Emit;
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
    public void Get_ReturnsVersionMetadata_WithBuildConfiguration()
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
    public void Get_BuildConfiguration_MatchesAssemblyConfigurationAttribute()
    {
        var uiApiAssembly = typeof(VersionController).Assembly;
        var expected = uiApiAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

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
        expected.ShouldNotBeNullOrEmpty();
        payload!.BuildConfiguration.ShouldBe(expected);
    }

    [Test]
    public void Get_BuildConfiguration_IsNullWhenAttributeAbsent()
    {
        var assemblyName = new AssemblyName("VersionMetadataTestAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        moduleBuilder.DefineType("Placeholder").CreateType();

        VersionMetadataReader.GetBuildConfiguration(assemblyBuilder).ShouldBeNull();
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
