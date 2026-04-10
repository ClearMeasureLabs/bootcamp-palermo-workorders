using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.ServiceDefaults;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class GlobalExceptionLoggingWebTests
{
    private const string ExceptionProbePath = "/api/_test/exception-probe";
    private const string ProbeExceptionMessage = "GlobalExceptionLoggingMiddleware test probe";

    [Test]
    public async Task Should_LogUnhandledException_And_ReturnProblemJson_When_ApiThrows()
    {
        await using var factory = new GlobalExceptionLoggingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ExceptionProbePath);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        var errors = factory.CapturedLogs.Where(e =>
            e.LogLevel == LogLevel.Error
            && e.Category == typeof(GlobalExceptionLoggingMiddleware).FullName).ToList();
        errors.Count.ShouldBeGreaterThanOrEqualTo(1);
        var entry = errors.First(e => e.Exception is InvalidOperationException);
        entry.Message.ShouldContain("/api/_test/exception-probe");
        entry.Message.ShouldContain("GET");
        entry.Exception!.Message.ShouldBe(ProbeExceptionMessage);
    }

    [Test]
    public async Task Should_PreserveExceptionHandlerBehavior_When_MiddlewareLogs()
    {
        await using var factory = new GlobalExceptionLoggingWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            var response = await client.GetAsync(ExceptionProbePath);
            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            mediaType.ShouldNotBeNull();
            mediaType!.ShouldContain("application/problem+json");
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status500InternalServerError);
        }
    }

    [Test]
    public async Task Should_IncludeSafeCorrelationContext_InStructuredLog_When_UnhandledException()
    {
        var expectedCorrelationId = Guid.NewGuid().ToString("D");
        await using var factory = new GlobalExceptionLoggingWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, expectedCorrelationId);

        await client.GetAsync(ExceptionProbePath);

        var errors = factory.CapturedLogs.Where(e =>
            e.LogLevel == LogLevel.Error
            && e.Category == typeof(GlobalExceptionLoggingMiddleware).FullName).ToList();
        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain(expectedCorrelationId);
    }

    [Test]
    public async Task Should_NotEmitRawSecrets_InCapturedLogs_When_ApiThrows()
    {
        const string secret = "super-secret-token";
        await using var factory = new GlobalExceptionLoggingWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);

        await client.GetAsync(ExceptionProbePath);

        var combined = string.Join(
            "|",
            factory.CapturedLogs.Select(e => e.Message + (e.Exception?.ToString() ?? "")));
        combined.ShouldNotContain(secret);
    }

    [Test]
    public async Task Should_NotDuplicateErrorLogs_When_ProblemDetailsHandlerUnchanged()
    {
        await using var factory = new GlobalExceptionLoggingWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.GetAsync(ExceptionProbePath);

        var middlewareErrors = factory.CapturedLogs.Count(e =>
            e.LogLevel == LogLevel.Error
            && e.Category == typeof(GlobalExceptionLoggingMiddleware).FullName);
        middlewareErrors.ShouldBe(1);

        var problemHandlerErrors = factory.CapturedLogs.Count(e =>
            e.LogLevel == LogLevel.Error
            && e.Category == typeof(ProblemDetailsExceptionHandler).FullName);
        problemHandlerErrors.ShouldBe(0);
    }
}
