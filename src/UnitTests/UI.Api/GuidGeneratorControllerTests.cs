using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    private static GuidGeneratorController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Test]
    public void Post_Should_Return200WithOneGuid_When_BodyOmitted()
    {
        var result = CreateController().Post(null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var response = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        response.Guids.Count.ShouldBe(1);
        Guid.TryParse(response.Guids[0], out _).ShouldBeTrue();
        response.Guids[0].ShouldBe(Guid.Parse(response.Guids[0]).ToString("D"));
    }

    [Test]
    public void Post_Should_Return200WithOneGuid_When_CountAbsent()
    {
        var result = CreateController().Post(new GuidGeneratorRequest());

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        response.Guids.Count.ShouldBe(1);
    }

    [Test]
    public void Post_Should_Return200WithRequestedCount_When_CountIsExplicit()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(5));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<GuidGeneratorResponse>().Guids.Count.ShouldBe(5);
    }

    [Test]
    public void Post_Should_Return200WithOneGuid_When_CountIsOne()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(1));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<GuidGeneratorResponse>().Guids.Count.ShouldBe(1);
    }

    [Test]
    public void Post_Should_Return200With100Guids_When_CountIs100()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(100));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<GuidGeneratorResponse>().Guids.Count.ShouldBe(100);
    }

    [Test]
    public void Post_Should_Return400Problem_When_CountIsZero()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(0));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail.ShouldContain("1");
        details.Detail.ShouldContain("100");
    }

    [Test]
    public void Post_Should_Return400Problem_When_CountIs101()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(101));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail.ShouldContain("100");
    }

    [Test]
    public void Post_Should_ReturnDistinctGuids_When_CountGreaterThanOne()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(10));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var guids = ok.Value.ShouldBeOfType<GuidGeneratorResponse>().Guids;
        guids.Distinct().Count().ShouldBe(10);
    }

    [Test]
    public void Post_Should_ReturnValidGuidFormat_When_Success()
    {
        var result = CreateController().Post(new GuidGeneratorRequest(3));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        foreach (var guid in ok.Value.ShouldBeOfType<GuidGeneratorResponse>().Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
            guid.ShouldBe(Guid.Parse(guid).ToString("D"));
        }
    }
}
