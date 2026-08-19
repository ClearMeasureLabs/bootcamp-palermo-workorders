using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusControllerTests
{
    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Test]
    public void Get_Should_ReturnOk_WithExpectedJsonShape()
    {
        var controller = CreateController("UnitTestEnv");

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldNotBeNullOrWhiteSpace();
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldNotBeNullOrWhiteSpace();
        payload.HostEnvironmentName.ShouldBe("UnitTestEnv");
        payload.EnvironmentVariables.ShouldNotBeNull();
    }

    [Test]
    public void Get_Should_RedactEnvironmentVariableValues_When_VariablesAreSet()
    {
        const string varName = "8457_CTRL_REDACT";
        const string secret = "controller-secret-8457";
        Environment.SetEnvironmentVariable(varName, secret);
        try
        {
            Environment.GetEnvironmentVariable(varName).ShouldBe(secret);
            var options = new EnvironmentStatusOptions { MonitoredVariables = [varName] };
            var controller = CreateController("UnitTestHostEnv", options);
            var expectedPayload = EnvironmentStatusBuilder.Build(new StubHostEnvironment("UnitTestHostEnv"), options);

            var result = controller.Get();
            var content = result.ShouldBeOfType<ContentResult>();
            content.Content!.ShouldNotContain(secret);

            var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
                content.Content!,
                ConditionalGetEtag.JsonSerializerOptions);
            payload.ShouldNotBeNull();
            payload!.EnvironmentVariables.Count.ShouldBe(expectedPayload.EnvironmentVariables.Count);
            payload.EnvironmentVariables.ShouldContainKey(varName);
            payload.EnvironmentVariables[varName].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
            payload.EnvironmentVariables.Values.ShouldNotContain(secret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
    }

    [Test]
    public void Get_Should_OmitUnsetEnvironmentVariables_When_NotPresent()
    {
        const string unsetName = "8457_CTRL_UNSET";
        Environment.SetEnvironmentVariable(unsetName, null);
        var options = new EnvironmentStatusOptions { MonitoredVariables = [unsetName] };
        var controller = CreateController("OmitTest8457", options);

        var result = controller.Get();
        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldNotContainKey(unsetName);
    }

    private static EnvironmentStatusController CreateController(
        string environmentName,
        EnvironmentStatusOptions? options = null)
    {
        return new EnvironmentStatusController(
            new StubHostEnvironment(environmentName),
            Options.Create(options ?? new EnvironmentStatusOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
