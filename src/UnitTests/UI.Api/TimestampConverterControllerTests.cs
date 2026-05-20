using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterControllerTests
{
    private static TimestampConverterController Controller()
    {
        return new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Test]
    public void Get_Should_Return200JsonAndEpochRoundTrip_When_ValidUnixSeconds()
    {
        var sut = Controller();
        var result = sut.Get(epoch: 1774872000, iso: null);

        var json = ShouldBeOkJson(result);
        json.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(1774872000);
        json.RootElement.GetProperty("unixEpochMilliseconds").GetInt64().ShouldBe(1774872000000);
        json.RootElement.GetProperty("iso8601Utc").GetString().ShouldNotBeNull();
        json.RootElement.GetProperty("iso8601Utc").GetString()!.ShouldContain("2026-03-30");
        json.RootElement.GetProperty("rfc1123Utc").GetString().ShouldNotBeNullOrWhiteSpace();
        var utcDisplay = json.RootElement.GetProperty("utcDisplay").GetString();
        utcDisplay.ShouldNotBeNull();
        utcDisplay!.ShouldContain("March");
        utcDisplay.ShouldContain("UTC");
    }

    [Test]
    public void Get_Should_Return200Json_When_ValidUnixMilliseconds()
    {
        var sut = Controller();
        var result = sut.Get(epoch: 1774872000000L, iso: null);

        var json = ShouldBeOkJson(result);
        json.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(1774872000);
    }

    [Test]
    public void Get_Should_Return200Json_When_ValidIso()
    {
        var sut = Controller();
        var result = sut.Get(epoch: null, iso: "2026-03-30T12:00:00.0000000+00:00");

        var json = ShouldBeOkJson(result);
        json.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(1774872000);
    }

    [Test]
    public void Get_Should_ReturnProblem_When_NeitherProvided()
    {
        var sut = Controller();
        var result = sut.Get(epoch: null, iso: null);
        ShouldBeProblem(result, StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_ReturnProblem_When_BothProvided()
    {
        var sut = Controller();
        var result = sut.Get(epoch: 0, iso: "2026-03-30T12:00:00Z");
        ShouldBeProblem(result, StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_ReturnProblem_When_IsoMalformed()
    {
        var sut = Controller();
        var result = sut.Get(epoch: null, iso: "not-a-date");
        ShouldBeProblem(result, StatusCodes.Status400BadRequest);
    }

    [Test]
    public void Get_Should_ReturnProblem_When_EpochOutOfRange()
    {
        var sut = Controller();
        var result = sut.Get(epoch: long.MaxValue, iso: null);
        ShouldBeProblem(result, StatusCodes.Status400BadRequest);
    }

    private static JsonDocument ShouldBeOkJson(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        return JsonDocument.Parse(content.Content.ShouldNotBeNull());
    }

    private static void ShouldBeProblem(IActionResult result, int expectedStatus)
    {
        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(expectedStatus);
        problem.Value.ShouldBeOfType<ProblemDetails>()!.Detail.ShouldNotBeNullOrWhiteSpace();
    }
}
