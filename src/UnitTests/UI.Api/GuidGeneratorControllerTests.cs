using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class GuidGeneratorControllerTests
{
    [Test]
    public void Post_Should_ReturnOneGuid_When_CountOmitted()
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(null, null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<GuidGeneratorResponse>();
        var payload = (GuidGeneratorResponse)ok.Value!;
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public void Post_Should_ReturnRequestedCount_When_QueryCountProvided()
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(null, 5);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = (GuidGeneratorResponse)ok.Value!;
        payload.Guids.Count.ShouldBe(5);
        payload.Guids.Select(g => Guid.TryParse(g, out var id) ? id : Guid.Empty).Distinct().Count().ShouldBe(5);
    }

    [Test]
    public void Post_Should_PreferBodyCount_When_BodyAndQueryProvided()
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(new GuidGeneratorRequest(2), 9);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = (GuidGeneratorResponse)ok.Value!;
        payload.Guids.Count.ShouldBe(2);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    public void Post_Should_Return400_When_CountOutOfRange(int count)
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(null, count);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_SerializeGuidsAsCamelCase_When_JsonSerialized()
    {
        var controller = new GuidGeneratorController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Post(null, 1);
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(ok.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        json.ShouldContain("\"guids\"");
    }
}
