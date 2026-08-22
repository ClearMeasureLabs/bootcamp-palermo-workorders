using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterControllerTests
{
    private const long KnownEpochSeconds = 1711800000L;
    private static readonly DateTimeOffset KnownInstant =
        DateTimeOffset.FromUnixTimeSeconds(KnownEpochSeconds);

    private sealed class StubProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = default,
            string? title = default,
            string? type = default,
            string? detail = default,
            string? instance = default) =>
            new() { Status = statusCode ?? 400, Detail = detail, Title = title ?? "Problem" };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = default,
            string? title = default,
            string? type = default,
            string? detail = default,
            string? instance = default) =>
            new(modelStateDictionary) { Status = statusCode ?? 400 };
    }

    private static TimestampConverterController CreateController()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<ProblemDetailsFactory, StubProblemDetailsFactory>()
                .BuildServiceProvider()
        };

        return new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static TimestampConverterResponse AssertJsonOk(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }

    [Test]
    public void Get_Should_ReturnEpochAndIso_When_ValueIsUnixSeconds()
    {
        var controller = CreateController();

        var payload = AssertJsonOk(controller.Get(KnownEpochSeconds.ToString(CultureInfo.InvariantCulture)));

        payload.UnixEpochSeconds.ShouldBe(KnownEpochSeconds);
        payload.UnixEpochMilliseconds.ShouldBe(KnownEpochSeconds * 1000L);
        payload.Iso8601Utc.ShouldBe(KnownInstant.ToString("O", CultureInfo.InvariantCulture));
        payload.Rfc1123Utc.ShouldBe(KnownInstant.ToString("R", CultureInfo.InvariantCulture));
        payload.UtcDisplay.ShouldContain("UTC");
    }

    [Test]
    public void Get_Should_ReturnEpochAndIso_When_ValueIsUnixMilliseconds()
    {
        var controller = CreateController();
        var ms = KnownEpochSeconds * 1000L;

        var payload = AssertJsonOk(controller.Get(ms.ToString(CultureInfo.InvariantCulture)));

        payload.UnixEpochSeconds.ShouldBe(KnownEpochSeconds);
        payload.UnixEpochMilliseconds.ShouldBe(ms);
        payload.Iso8601Utc.ShouldBe(KnownInstant.ToString("O", CultureInfo.InvariantCulture));
        payload.UtcDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_Should_ReturnEpochAndIso_When_ValueIsIso8601()
    {
        var controller = CreateController();
        var iso = KnownInstant.ToString("O", CultureInfo.InvariantCulture);

        var payload = AssertJsonOk(controller.Get(iso));

        payload.UnixEpochSeconds.ShouldBe(KnownEpochSeconds);
        payload.UnixEpochMilliseconds.ShouldBe(KnownEpochSeconds * 1000L);
        payload.Iso8601Utc.ShouldBe(iso);
        payload.UtcDisplay.ShouldContain("UTC");
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_ValueMissing()
    {
        var controller = CreateController();

        var result = controller.Get(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("value");
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_ValueInvalid()
    {
        var controller = CreateController();

        var result = controller.Get("not-a-timestamp");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
    }
}
