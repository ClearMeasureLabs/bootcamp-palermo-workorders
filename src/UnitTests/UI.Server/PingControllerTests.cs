using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class PingControllerTests
{
    [Test]
    public void Get_ShouldReturnOkWithPong()
    {
        var controller = new PingController();

        var result = controller.Get();

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo("pong"));
    }
}
