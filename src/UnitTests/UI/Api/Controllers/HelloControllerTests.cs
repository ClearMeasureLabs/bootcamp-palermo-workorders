using ClearMeasure.Bootcamp.UI.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Controllers;

public class HelloControllerTests
{
    [Fact]
    public void Get_Should_ReturnJsonWithHelloWorldMessage()
    {
        // Arrange
        var controller = new HelloController();

        // Act
        var result = controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var messageProperty = value!.GetType().GetProperty("message");
        messageProperty.Should().NotBeNull();
        messageProperty!.GetValue(value).Should().Be("Hello, World!");
    }

    [Fact]
    public void Get_Should_ReturnExpectedMessageContent()
    {
        // Arrange
        var controller = new HelloController();

        // Act
        var result = controller.Get() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var value = result!.Value;
        var messageProperty = value!.GetType().GetProperty("message");
        var messageValue = messageProperty!.GetValue(value) as string;
        
        messageValue.Should().Be("Hello, World!");
    }
}
