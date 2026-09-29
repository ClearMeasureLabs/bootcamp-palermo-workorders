using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterControllerTests
{
    private const long KnownUnixSeconds = 1_700_000_000L;
    private const string KnownIso = "2023-11-14T22:13:20Z";
    private const string KnownHuman = "Tuesday, 14 November 2023 22:13:20 UTC";

    private const string MissingParamDetail =
        "Provide exactly one of query parameters 'unix' (epoch seconds) or 'iso' (ISO-8601).";

    private const string BothParamsDetail =
        "Provide exactly one of query parameters 'unix' (epoch seconds) or 'iso' (ISO-8601), not both.";

    private const string UnixInvalidDetail =
        "Query parameter 'unix' must be a 64-bit integer Unix timestamp in seconds (not milliseconds).";

    private const string UnixMillisecondsDetail =
        "Query parameter 'unix' must be Unix epoch seconds, not milliseconds. Received a millisecond-scale value.";

    private const string IsoInvalidDetail =
        "Query parameter 'iso' must be a valid ISO-8601 date/time string.";

    [Test]
    public void Get_Should_ReturnGoldenJson_When_UnixSeconds()
    {
        var result = CreateController(("unix", KnownUnixSeconds.ToString())).Get(KnownUnixSeconds.ToString(), null);

        var payload = AssertOkPayload(result);
        AssertGolden(payload);
    }

    [Test]
    public void Get_Should_ReturnGoldenJson_When_Iso8601()
    {
        var result = CreateController(("iso", KnownIso)).Get(null, KnownIso);

        var payload = AssertOkPayload(result);
        AssertGolden(payload);
    }

    [Test]
    public void Get_Should_Return400_When_QueryMissing()
    {
        var result = CreateController().Get(null, null);

        AssertProblem400(result, MissingParamDetail);
    }

    [Test]
    public void Get_Should_Return400_When_BothUnixAndIso()
    {
        var result = CreateController(("unix", KnownUnixSeconds.ToString()), ("iso", KnownIso))
            .Get(KnownUnixSeconds.ToString(), KnownIso);

        AssertProblem400(result, BothParamsDetail);
    }

    [Test]
    public void Get_Should_Return400_When_UnixInvalid()
    {
        var result = CreateController(("unix", "not-a-number")).Get("not-a-number", null);

        AssertProblem400(result, UnixInvalidDetail);
    }

    [Test]
    public void Get_Should_Return400_When_UnixMillisecondsNotAutodetected()
    {
        var result = CreateController(("unix", "1700000000000")).Get("1700000000000", null);

        AssertProblem400(result, UnixMillisecondsDetail);
    }

    [Test]
    public void Get_Should_Return400_When_IsoInvalid()
    {
        var result = CreateController(("iso", "not-an-iso-timestamp")).Get(null, "not-an-iso-timestamp");

        AssertProblem400(result, IsoInvalidDetail);
    }

    private static TimestampConverterResponse AssertOkPayload(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        return ok.Value.ShouldBeOfType<TimestampConverterResponse>();
    }

    private static void AssertGolden(TimestampConverterResponse payload)
    {
        payload.Unix.ShouldBe(KnownUnixSeconds);
        payload.Iso.ShouldBe(KnownIso);
        payload.Human.ShouldBe(KnownHuman);
    }

    private static void AssertProblem400(IActionResult result, string expectedDetail)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe(expectedDetail);
    }

    private static TimestampConverterController CreateController(params (string Key, string Value)[] query)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var (key, value) in query)
        {
            httpContext.Request.QueryString = httpContext.Request.QueryString.Add(key, value);
        }

        return new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
