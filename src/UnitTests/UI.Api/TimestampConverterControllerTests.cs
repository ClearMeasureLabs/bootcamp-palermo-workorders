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
    [Test]
    public void Should_Return200WithJson_When_EpochQueryProvided()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "1711792800", iso: null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.InputKind.ShouldBe("epoch");
        payload.EpochSeconds.ShouldBe(1711792800);
        payload.Iso8601Utc.ShouldNotBeNullOrWhiteSpace();
        payload.Utc.ShouldNotBeNullOrWhiteSpace();
        payload.Local.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_Return200WithJson_When_IsoQueryProvided()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: null, iso: "2026-07-12T15:00:00Z");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.InputKind.ShouldBe("iso");
    }

    [Test]
    public void Should_Return400Problem_When_NeitherParameterProvided()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: null, iso: null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("exactly one");
    }

    [Test]
    public void Should_Return400Problem_When_BothParametersProvided()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "1", iso: "2026-01-01T00:00:00Z");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("mutually exclusive");
    }

    [Test]
    public void Should_Return400Problem_When_EpochInvalid()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "abc", iso: null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_Return400Problem_When_IsoInvalid()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: null, iso: "garbage");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("ISO-8601");
    }

    private static TimestampConverterController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
