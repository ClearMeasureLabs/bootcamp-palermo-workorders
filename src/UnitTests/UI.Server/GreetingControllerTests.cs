using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class GreetingControllerTests
{
    [Test]
    public void Get_ShouldReturnOkResultWithGreetingMessage()
    {
        // Arrange
        var controller = new GreetingController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
        
        var value = okResult.Value;
        var messageProperty = value!.GetType().GetProperty("message");
        Assert.That(messageProperty, Is.Not.Null);
        Assert.That(messageProperty!.GetValue(value), Is.EqualTo("Hello from Church Bulletin"));
    }

    [Test]
    public void Get_ShouldReturnJsonWithMessageProperty()
    {
        // Arrange
        var controller = new GreetingController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        Assert.That(value, Is.Not.Null);
        
        var messageProperty = value!.GetType().GetProperty("message");
        Assert.That(messageProperty, Is.Not.Null);
        Assert.That(messageProperty!.GetValue(value), Is.EqualTo("Hello from Church Bulletin"));
    }
}
