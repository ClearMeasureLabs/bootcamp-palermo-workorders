using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterControllerTests
{
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

    private static TimestampConverterController CreateController(HttpContext? httpContext = null)
    {
        httpContext ??= new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<ProblemDetailsFactory, StubProblemDetailsFactory>()
            .BuildServiceProvider();

        return new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Test]
    public void Get_Should_ReturnJson_When_UnixEpochSeconds()
    {
        var controller = CreateController();

        var result = controller.Get(iso: null, unix: "1609459200");

        var content = AssertJsonOk(result);
        content.UtcIso8601.ShouldBe("2021-01-01T00:00:00.0000000Z");
        content.UnixSeconds.ShouldBe(1609459200L);
        content.UnixMilliseconds.ShouldBe(1609459200000L);
        content.UtcRfc1123.ShouldBe("Fri, 01 Jan 2021 00:00:00 GMT");
        content.UtcInvariantFormatted.ShouldBe(
            new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("F", CultureInfo.InvariantCulture));
    }

    [Test]
    public void Get_Should_UseMilliseconds_When_AbsUnixPastThreshold()
    {
        var controller = CreateController();

        var result = controller.Get(iso: null, unix: "1609459200000");

        var content = AssertJsonOk(result);
        content.UnixMilliseconds.ShouldBe(1609459200000L);
    }

    [Test]
    public void Get_Should_UseSeconds_When_ValueBelowMillisecondsThresholdAndInRange()
    {
        var controller = CreateController();

        var resultSeconds = controller.Get(iso: null, unix: "2000000000");

        AssertJsonOk(resultSeconds).UnixSeconds.ShouldBe(2000000000L);

        var resultMs = controller.Get(iso: null, unix: "2000000000000");

        var msPayload = AssertJsonOk(resultMs);
        msPayload.UtcIso8601.ShouldBe("2033-05-18T03:33:20.0000000Z");
    }

    [Test]
    public void Get_Should_MapSameUtcInstant_ForEquivalentSecondsVersusMillisecondsRepresentations()
    {
        var controller = CreateController();

        var fromSeconds = AssertJsonOk(controller.Get(iso: null, unix: "1609459200"));

        var millisecondsController = CreateController();

        var fromMilliseconds = AssertJsonOk(millisecondsController.Get(iso: null, unix: "1609459200000"));

        fromSeconds.UtcIso8601.ShouldBe(fromMilliseconds.UtcIso8601);
        fromSeconds.UnixSeconds.ShouldBe(fromMilliseconds.UnixSeconds);
        fromMilliseconds.UnixMilliseconds.ShouldBe(1609459200000L);
    }

    [Test]
    public void Get_Should_TreatHugeNumericAsMilliseconds_ButHugeSecondsFailWhenOutOfRange()
    {
        var controller = CreateController();

        var outOfRangeSeconds = controller.Get(iso: null, unix: "253402400000");
        ((ObjectResult)outOfRangeSeconds).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_DistinguishInterpretationAcrossScaleWhenInstantsWouldDifferAtSameDigits()
    {
        var asSecondsController = CreateController();

        var asSecondsPayload = AssertJsonOk(asSecondsController.Get(iso: null, unix: "1700000000"));

        var millisecondsController = CreateController();

        var millisecondsPayload =
            AssertJsonOk(millisecondsController.Get(iso: null, unix: "1700000000000"));

        asSecondsPayload.UtcIso8601.ShouldBe(millisecondsPayload.UtcIso8601);
        asSecondsPayload.UtcIso8601.ShouldBe("2023-11-14T22:13:20.0000000Z");
        millisecondsPayload.UnixMilliseconds.ShouldBe(1700000000000L);
    }

    [Test]
    public void Get_Should_ReturnJson_When_IsoRoundTripUtc()
    {
        var controller = CreateController();

        var content = AssertJsonOk(controller.Get(iso: "2026-03-30T12:00:00.0000000Z", unix: null));
        content.UtcIso8601.ShouldBe("2026-03-30T12:00:00.0000000Z");
        content.UnixSeconds.ShouldBe(1774872000L);
    }

    [Test]
    public void Get_Should_Return400_When_BothQueriesSupplied()
    {
        var controller = CreateController();

        var result = controller.Get(iso: "2026-03-30T12:00:00.0000000Z", unix: "0");

        var problem = result.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_Return400_When_NeitherQuerySupplied()
    {
        var controller = CreateController();

        var result = controller.Get(iso: null, unix: null);

        ((ObjectResult)result).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_Return400_When_EmptyIsoWhitespaceOnly()
    {
        var controller = CreateController();

        var result = controller.Get(iso: "   ", unix: null);

        ((ObjectResult)result).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_Return400_When_InvalidUnixNumeric()
    {
        var controller = CreateController();

        var result = controller.Get(iso: null, unix: "not-a-number");

        ((ObjectResult)result).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_Return400_When_InvalidIsoGarbage()
    {
        var controller = CreateController();

        var result = controller.Get(iso: "not-a-ts", unix: null);

        ((ObjectResult)result).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesWeakEtag()
    {
        var httpContext = new DefaultHttpContext();
        var controller = CreateController(httpContext);

        var first = controller.Get(iso: null, unix: "0");
        var content = AssertJsonOk(first);

        httpContext.Response.Headers.Remove(HeaderNames.ETag);
        controller = CreateController(httpContext);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(content);
        httpContext.Request.Headers.IfNoneMatch = etag.ToString();

        var second = controller.Get(iso: null, unix: "0");

        second.ShouldBeOfType<StatusCodeResult>()
            .StatusCode.ShouldBe(StatusCodes.Status304NotModified);
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
}
