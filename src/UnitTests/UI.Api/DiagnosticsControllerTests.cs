using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
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
        var controller = new DiagnosticsController(
            stubEnv,
            clock,
            Options.Create(flags));

        var result = controller.Get();

        var json = result.ShouldBeOfType<JsonResult>();
        json.Value.ShouldBeOfType<DiagnosticsResponse>();
        var payload = (DiagnosticsResponse)json.Value!;
        payload.Environment.ShouldBe("UnitTestEnv");
        payload.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
        payload.FeatureFlags.SampleFeatureA.ShouldBeTrue();
        payload.FeatureFlags.SampleFeatureB.ShouldBeFalse();
    }
}
