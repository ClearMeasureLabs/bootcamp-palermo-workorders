using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DetailedHealthControllerTests
{
    private sealed class FixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class StubDetailedHealthReportProvider(DetailedHealthReport report) : IDetailedHealthReportProvider
    {
        public Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(report);
    }

    private static DetailedHealthReport FixedReport(DateTime checkedAtUtc) => new()
    {
        OverallStatus = ComponentHealthStatus.Healthy,
        CheckedAtUtc = checkedAtUtc,
        ProcessId = 42,
        OsDescription = "Test OS",
        FrameworkDescription = "Test Framework",
        GcMemoryMb = 100,
        WorkingSetMb = 200,
        ProcessorCount = 4,
        Is64BitProcess = true,
        TimeZoneId = "UTC",
        ProcessPriority = "Normal",
        Components =
        [
            new ComponentHealthEntry { Name = "API", Status = ComponentHealthStatus.Healthy }
        ]
    };

    [Test]
    public async Task GetDetailed_Should_SetEtagHeader_When_ReportReturned()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var controller = CreateController(fixedTime);

        var result = await controller.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        controller.Response.Headers.ETag.ToString().ShouldStartWith("W/\"");
    }

    [Test]
    public async Task GetDetailed_Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var report = FixedReport(fixedTime);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(report);

        var controller = CreateController(fixedTime);
        controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = etag.ToString();

        var result = await controller.GetDetailed(CancellationToken.None);

        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public async Task GetDetailed_Should_Return200_When_IfNoneMatchDiffers()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var controller = CreateController(fixedTime);
        controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = "W/\"stale-etag\"";

        var result = await controller.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.Content.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task GetDetailed_Should_ReturnJsonPayload_When_ReportReturned()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var controller = CreateController(fixedTime);

        var result = await controller.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<DetailedHealthReport>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OverallStatus.ShouldBe(ComponentHealthStatus.Healthy);
        payload.Components.Count.ShouldBe(1);
        payload.Components[0].Name.ShouldBe("API");
    }

    private static DetailedHealthController CreateController(DateTime fixedTime)
    {
        var report = FixedReport(fixedTime);
        var controller = new DetailedHealthController(
            new FixedUtcTimeProvider(fixedTime),
            new StubDetailedHealthReportProvider(report))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }
}
