using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    private static GuidGeneratorController CreateController()
    {
        return new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Test]
    public void Post_Should_ReturnOneGuid_When_CountOmitted()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(1);
        _ = Guid.Parse(payload.Guids[0]);
    }

    [Test]
    public void Post_Should_ReturnNGuids_When_CountSpecified()
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(5));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        payload.Guids.Count.ShouldBe(5);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in payload.Guids)
        {
            set.Add(g).ShouldBeTrue();
            _ = Guid.Parse(g);
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    public void Post_Should_Return400_When_CountOutOfRange(int count)
    {
        var controller = CreateController();

        var result = controller.Post(new GuidGeneratorRequest(count));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        problem.Value.ShouldBeOfType<ProblemDetails>();
    }
}
