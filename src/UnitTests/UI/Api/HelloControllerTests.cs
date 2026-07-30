using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonWithHelloWorldMessage()
    {
        // Arrange
        var controller = new HelloController();

        // Act
        var result = controller.Get();

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.ShouldBe(200);
        
        var value = okResult.Value;
        value.ShouldNotBeNull();
        
        var messageProperty = value!.GetType().GetProperty("message");
        messageProperty.ShouldNotBeNull();
        messageProperty!.GetValue(value).ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_ReturnExpectedMessageContent()
    {
        // Arrange
        var controller = new HelloController();

        // Act
        var result = controller.Get() as OkObjectResult;

        // Assert
        result.ShouldNotBeNull();
        var value = result!.Value;
        var messageProperty = value!.GetType().GetProperty("message");
        var messageValue = messageProperty!.GetValue(value) as string;
        
        messageValue.ShouldBe("Hello, World!");
    }
}
