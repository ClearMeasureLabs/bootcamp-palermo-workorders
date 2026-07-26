using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonGreeting_With200Status()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var responseObject = okResult.Value.ShouldNotBeNull();
        var messageProperty = responseObject.GetType().GetProperty("message");
        messageProperty.ShouldNotBeNull();
        var messageValue = messageProperty.GetValue(responseObject);
        messageValue.ShouldBe("Hello, World!");
    }
}
