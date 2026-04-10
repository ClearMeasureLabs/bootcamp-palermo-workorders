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
public class DiagnosticsControllerTests
{
    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Get_Should_ReturnJson_WithEnvironmentUptimeAndFeatureFlags_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var stubEnv = new StubHostEnvironment("UnitTestEnv");
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };
        var controller = new DiagnosticsController(stubEnv, clock, Options.Create(flags))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<DiagnosticsResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Environment.ShouldBe("UnitTestEnv");
        payload.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
        payload.FeatureFlags.SampleFeatureA.ShouldBeTrue();
        payload.FeatureFlags.SampleFeatureB.ShouldBeFalse();
    }
}
