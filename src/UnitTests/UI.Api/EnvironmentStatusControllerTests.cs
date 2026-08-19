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
    [Test]
    public void Get_Should_ReturnOk_WithExpectedJsonShape()
    {
        var controller = CreateController(new StubHostEnvironment("TestEnvironment"));

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldNotBeNullOrEmpty();
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ClrVersion.ShouldNotBeNullOrEmpty();
        payload.HostEnvironmentName.ShouldBe("TestEnvironment");
        payload.EnvironmentVariables.ShouldNotBeNull();
    }

    [Test]
    public void Get_Should_RedactEnvironmentVariableValues_When_VariablesAreSet()
    {
        const string rawAspNetCoreEnvironment = "SecretAspNetCoreValue";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", rawAspNetCoreEnvironment);
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", "SQL-Container");
        try
        {
            var controller = CreateController(new StubHostEnvironment("TestEnvironment"));

            var result = controller.Get();
            var content = result.ShouldBeOfType<ContentResult>();
            content.Content!.ShouldNotContain(rawAspNetCoreEnvironment);
            content.Content.ShouldNotContain("SQL-Container");
            content.Content.ShouldContain(EnvironmentStatusBuilder.RedactedValue);

            var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
                content.Content,
                ConditionalGetEtag.JsonSerializerOptions);
            payload.ShouldNotBeNull();
            payload!.EnvironmentVariables.ContainsKey("ASPNETCORE_ENVIRONMENT").ShouldBeTrue();
            payload.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
            payload.EnvironmentVariables.ContainsKey("DATABASE_ENGINE").ShouldBeTrue();
            payload.EnvironmentVariables["DATABASE_ENGINE"].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DATABASE_ENGINE", null);
        }
    }

    [Test]
    public void Get_Should_OmitUnsetEnvironmentVariables_When_NotPresent()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        try
        {
            var controller = CreateController(new StubHostEnvironment("TestEnvironment"));

            var result = controller.Get();
            var content = result.ShouldBeOfType<ContentResult>();

            content.Content!.ShouldNotContain("otelExporterOtlpEndpoint");
            var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
                content.Content,
                ConditionalGetEtag.JsonSerializerOptions);
            payload.ShouldNotBeNull();
            payload!.EnvironmentVariables.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT").ShouldBeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        }
    }

    private static EnvironmentStatusController CreateController(IHostEnvironment hostEnvironment) =>
        new(hostEnvironment, Options.Create(new EnvironmentStatusOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
