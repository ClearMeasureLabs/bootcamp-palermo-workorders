using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithCorrectJsonShape()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<OkObjectResult>();
        content.StatusCode.ShouldBe(200);
        content.Value.ShouldNotBeNull();

        dynamic obj = content.Value;
        ((string)obj.message).ShouldBe("Hello, World!");
    }
}
