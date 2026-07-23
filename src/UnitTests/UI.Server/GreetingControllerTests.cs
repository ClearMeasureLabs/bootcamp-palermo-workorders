using ClearMeasure.Bootcamp.UI.Server.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class GreetingControllerTests
{
    [Fact]
    public void Get_ShouldReturnOkResultWithGreetingMessage()
    {
        // Arrange
        var controller = new GreetingController();

        // Act
        var result = controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(new { message = "Hello from Church Bulletin" });
    }

    [Fact]
    public void Get_ShouldReturnJsonWithMessageProperty()
    {
        // Arrange
        var controller = new GreetingController();

        // Act
        var result = controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var messageProperty = value!.GetType().GetProperty("message");
        messageProperty.Should().NotBeNull();
        messageProperty!.GetValue(value).Should().Be("Hello from Church Bulletin");
    }
}
