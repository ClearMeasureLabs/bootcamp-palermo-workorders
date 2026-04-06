using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class DetailedHealthCheckResponseWriterTests
{
    [Test]
    public async Task WriteAsync_Should_WriteJsonWithOverallStatusAndEntries()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(
                HealthStatus.Healthy,
                "API layer is healthy",
                TimeSpan.FromMilliseconds(5),
                null,
                new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(5));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        doc.RootElement.GetProperty("overallStatus").GetString().ShouldBe("Healthy");
        doc.RootElement.GetProperty("totalDurationMs").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
        var entriesArray = doc.RootElement.GetProperty("entries");
        entriesArray.GetArrayLength().ShouldBe(1);
        entriesArray[0].GetProperty("name").GetString().ShouldBe("API");
        entriesArray[0].GetProperty("status").GetString().ShouldBe("Healthy");
        entriesArray[0].GetProperty("description").GetString().ShouldBe("API layer is healthy");
    }

    [Test]
    public async Task WriteAsync_Should_IncludeExceptionDetailsWhenUnhealthy()
    {
        var exception = new InvalidOperationException("Cannot connect");
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["DataAccess"] = new(
                HealthStatus.Unhealthy,
                "Database connection failed",
                TimeSpan.FromMilliseconds(150),
                exception,
                new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(150));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        doc.RootElement.GetProperty("overallStatus").GetString().ShouldBe("Unhealthy");
        var entry = doc.RootElement.GetProperty("entries")[0];
        entry.GetProperty("status").GetString().ShouldBe("Unhealthy");
        entry.GetProperty("exceptionMessage").GetString().ShouldBe("Cannot connect");
        entry.GetProperty("exceptionDetail").GetString().ShouldNotBeNull();
        entry.GetProperty("exceptionDetail").GetString()!.ShouldContain("InvalidOperationException");
    }

    [Test]
    public async Task WriteAsync_Should_IncludeDataDictionaryWhenPresent()
    {
        var data = new Dictionary<string, object> { ["Provider"] = "SqlServer" };
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["DataAccess"] = new(
                HealthStatus.Degraded,
                "Slow response",
                TimeSpan.FromMilliseconds(5000),
                null,
                data)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(5000));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        var entry = doc.RootElement.GetProperty("entries")[0];
        entry.GetProperty("data").GetProperty("Provider").GetString().ShouldBe("SqlServer");
    }

    [Test]
    public async Task WriteAsync_Should_OmitNullFieldsFromJson()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["API"] = new(
                HealthStatus.Healthy,
                null,
                TimeSpan.FromMilliseconds(1),
                null,
                new Dictionary<string, object>())
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(1));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        var entry = doc.RootElement.GetProperty("entries")[0];
        entry.TryGetProperty("exceptionMessage", out _).ShouldBeFalse();
        entry.TryGetProperty("exceptionDetail", out _).ShouldBeFalse();
        entry.TryGetProperty("description", out _).ShouldBeFalse();
        entry.TryGetProperty("data", out _).ShouldBeFalse();
    }
}
