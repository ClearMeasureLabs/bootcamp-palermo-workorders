using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    private static readonly Regex CanonicalGuidFormat = new(
        @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.Compiled);

    [Test]
    public void Post_Should_ReturnSingleGuid_When_RequestBodyOmitted()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var payload = AssertOkPayload(result);
        payload.Count.ShouldBe(1);
        payload.Guids.Count.ShouldBe(1);
        AssertValidCanonicalGuid(payload.Guids[0]);
    }

    [Test]
    public void Post_Should_ReturnSingleGuid_When_CountOmittedInBody()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest());

        var payload = AssertOkPayload(result);
        payload.Count.ShouldBe(1);
        payload.Guids.Count.ShouldBe(1);
        AssertValidCanonicalGuid(payload.Guids[0]);
    }

    [Test]
    public void Post_Should_ReturnRequestedCount_When_CountProvided()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(5));

        var payload = AssertOkPayload(result);
        payload.Count.ShouldBe(5);
        payload.Guids.Count.ShouldBe(5);
        foreach (var guid in payload.Guids)
        {
            AssertValidCanonicalGuid(guid);
        }

        payload.Guids.Distinct().Count().ShouldBe(5);
    }

    [Test]
    public void Post_Should_Return100Guids_When_CountAtMaximum()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(100));

        var payload = AssertOkPayload(result);
        payload.Count.ShouldBe(100);
        payload.Guids.Count.ShouldBe(100);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_CountBelowMinimum()
    {
        var controller = CreateController();

        var zeroResult = controller.Post(new GuidGeneratorRequest(0));
        AssertBadRequest(zeroResult);

        var negativeResult = controller.Post(new GuidGeneratorRequest(-1));
        AssertBadRequest(negativeResult);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_CountAboveMaximum()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(101));

        AssertBadRequest(result);
    }

    [Test]
    public void Post_Should_ReturnCanonicalGuidFormat_When_Success()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(3));

        var payload = AssertOkPayload(result);
        foreach (var guid in payload.Guids)
        {
            guid.Length.ShouldBe(36);
            guid[8].ShouldBe('-');
            guid[13].ShouldBe('-');
            guid[18].ShouldBe('-');
            guid[23].ShouldBe('-');
            AssertValidCanonicalGuid(guid);
        }
    }

    private static GuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static GuidGeneratorResponse AssertOkPayload(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        return payload;
    }

    private static void AssertBadRequest(IActionResult result)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldContain("count must be between 1 and 100");
    }

    private static void AssertValidCanonicalGuid(string guid)
    {
        Guid.TryParse(guid, out _).ShouldBeTrue();
        CanonicalGuidFormat.IsMatch(guid).ShouldBeTrue();
    }
}
