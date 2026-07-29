using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Shouldly;
using System.Reflection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Should_Get_ReturnOkResult()
    {
        // Arrange
        var controller = new HelloController();

        // Act
        var result = controller.Get();

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Should_Get_ReturnHelloWorldMessage()
    {
        // Arrange
        var controller = new HelloController();

        // Act
        var result = controller.Get() as OkObjectResult;

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldNotBeNull();
        
        var valueType = result.Value.GetType();
        var messageProperty = valueType.GetProperty("message");
        messageProperty.ShouldNotBeNull();
        
        var message = messageProperty.GetValue(result.Value) as string;
        message.ShouldBe("Hello, World!");
    }
}