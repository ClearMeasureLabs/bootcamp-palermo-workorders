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
    private static TimestampConverterController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Test]
    public void Should_Return400_When_NoQueryParameters()
    {
        var result = CreateController().Get(epoch: null, iso: null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("epoch or iso");
    }

    [Test]
    public void Should_Return400_When_BothEpochAndIsoProvided()
    {
        var result = CreateController().Get(epoch: "1718208000", iso: "2024-06-12T12:00:00Z");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("only one");
    }

    [Test]
    public void Should_Return400_When_EpochIsWhitespaceOnly()
    {
        var result = CreateController().Get(epoch: "  ", iso: null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("epoch or iso");
    }

    [Test]
    public void Should_ReturnJson200_When_ValidEpochProvided()
    {
        var result = CreateController().Get(epoch: "1718208000", iso: null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.InputKind.ShouldBe("epoch");
        payload.EpochSeconds.ShouldBe(1718208000L);
    }

    [Test]
    public void Should_ReturnJson200_When_ValidIsoProvided()
    {
        var result = CreateController().Get(epoch: null, iso: "2024-06-12T16:00:00Z");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.InputKind.ShouldBe("iso");
        payload.EpochSeconds.ShouldBe(1718208000L);
    }

    [Test]
    public void Should_Return400_When_InvalidEpochProvided()
    {
        var result = CreateController().Get(epoch: "abc", iso: null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_Return400_When_InvalidIsoProvided()
    {
        var result = CreateController().Get(epoch: null, iso: "bad");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
    }
}
