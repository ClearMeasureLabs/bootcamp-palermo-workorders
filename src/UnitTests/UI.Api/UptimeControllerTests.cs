using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class UptimeControllerTests
{
    [Test]
    public void Get_Should_ReturnUptimeInSeconds()
    {
        var controller = new UptimeController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        okResult.Value.ShouldNotBeNull();
        
        var uptimeData = okResult.Value.ShouldBeAssignableTo<dynamic>();
        long uptimeSeconds = uptimeData.uptimeSeconds;
        uptimeSeconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Get_Should_ReturnPositiveUptime()
    {
        var controller = new UptimeController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldNotBeNull();
        
        var uptimeData = okResult.Value!;
        var uptimeProperty = uptimeData.GetType().GetProperty("uptimeSeconds");
        uptimeProperty.ShouldNotBeNull();
        var uptimeSeconds = (long)uptimeProperty!.GetValue(uptimeData)!;
        uptimeSeconds.ShouldBeGreaterThan(0);
    }
}
