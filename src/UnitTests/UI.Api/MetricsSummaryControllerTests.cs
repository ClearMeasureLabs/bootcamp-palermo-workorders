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
public class MetricsSummaryControllerTests
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

    private sealed class StubRequestMetrics : IRequestMetrics
    {
        public long TotalRequestsServed { get; set; }

        public void IncrementTotalRequestsServed() => TotalRequestsServed++;
    }

    [Test]
    public void Get_ReturnsJson_WithMetricsAndCorrectContentType_When_Called()
    {
        var clock = new FixedUtcTimeProvider(DateTimeOffset.UtcNow.AddMinutes(5));
        var metrics = new StubRequestMetrics { TotalRequestsServed = 5 };
        var controller = new MetricsSummaryController(new StubHostEnvironment("UnitTestEnv"), clock, metrics)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Environment.ShouldBe("UnitTestEnv");
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        payload.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(0);
        payload.MemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen0Count.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1Count.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Get_IncludesRequestCount_From_IRequestMetrics_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var metrics = new StubRequestMetrics { TotalRequestsServed = 42 };
        var controller = new MetricsSummaryController(new StubHostEnvironment("UnitTestEnv"), clock, metrics)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.TotalRequestsServed.ShouldBe(42);
    }

    [Test]
    public void Get_RoundsMemoryAndGcValuesToIntegers_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var metrics = new StubRequestMetrics { TotalRequestsServed = 1 };
        var controller = new MetricsSummaryController(new StubHostEnvironment("UnitTestEnv"), clock, metrics)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("memoryMb").ValueKind.ShouldBe(JsonValueKind.Number);
        doc.RootElement.GetProperty("memoryMb").TryGetInt32(out _).ShouldBeTrue();
        var gc = doc.RootElement.GetProperty("gcCollectionCounts");
        gc.GetProperty("gen0Count").TryGetInt32(out _).ShouldBeTrue();
        gc.GetProperty("gen1Count").TryGetInt32(out _).ShouldBeTrue();
        gc.GetProperty("gen2Count").TryGetInt32(out _).ShouldBeTrue();
    }
}
