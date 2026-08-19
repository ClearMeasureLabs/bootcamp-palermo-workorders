using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonHelloWorld()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var json = result.ShouldBeOfType<JsonResult>();
        json.StatusCode.ShouldBe(200);
        json.Value.ShouldNotBeNull();
        var value = json.Value!;
        var messageProperty = value.GetType().GetProperty("message");
        messageProperty.ShouldNotBeNull();
        messageProperty!.GetValue(value).ShouldBe("Hello, World!");
    }
}
