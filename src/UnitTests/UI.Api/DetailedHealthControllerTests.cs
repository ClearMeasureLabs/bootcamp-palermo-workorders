using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DetailedHealthControllerTests
{
    private sealed class StubDetailedHealthReportProvider(DetailedHealthReport report) : IDetailedHealthReportProvider
    {
        public Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(report);
    }

    private sealed class FixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private static DetailedHealthReport CreateFixedReport() =>
        HealthReportBuilder.FromEntries(
            new FixedUtcTimeProvider(new DateTime(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc)),
            [new ComponentHealthEntry { Name = "API", Status = ComponentHealthStatus.Healthy }]);

    [Test]
    public async Task GetDetailed_Should_ReturnOk_WithEtagHeader()
    {
        var controller = CreateController(CreateFixedReport());

        var result = await controller.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task GetDetailed_Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var report = CreateFixedReport();
        var controller = CreateController(report);
        var first = await controller.GetDetailed(CancellationToken.None);
        first.ShouldBeOfType<ContentResult>();
        var etag = controller.Response.Headers.ETag.ToString();

        controller = CreateController(report);
        controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = etag;
        var second = await controller.GetDetailed(CancellationToken.None);

        second.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private static DetailedHealthController CreateController(DetailedHealthReport report)
    {
        return new DetailedHealthController(
            new FixedUtcTimeProvider(new DateTime(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc)),
            new StubDetailedHealthReportProvider(report))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
