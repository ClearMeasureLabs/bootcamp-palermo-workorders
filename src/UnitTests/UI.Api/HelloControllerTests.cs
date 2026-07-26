using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonGreeting()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var value = okResult.Value;
        value.ShouldNotBeNull();

        var objType = value.GetType();
        var messageProp = objType.GetProperty("message");
        messageProp.ShouldNotBeNull();

        var messageValue = messageProp!.GetValue(value);
        messageValue.ShouldBe("Hello, World!");
    }
}
