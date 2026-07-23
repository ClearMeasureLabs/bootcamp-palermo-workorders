using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HealthzControllerTests
{
    [Test]
    public void Get_Should_Return200WithEmptyBody()
    {
        var controller = new HealthzController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkResult>();
        ok.StatusCode.ShouldBe(200);
    }
}
