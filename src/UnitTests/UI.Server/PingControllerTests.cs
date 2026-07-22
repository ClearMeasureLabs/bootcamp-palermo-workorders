using ClearMeasure.Bootcamp.UI.Server.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class PingControllerTests
{
    [Fact]
    public void Get_ShouldReturnOkWithPong()
    {
        var controller = new PingController();

        var result = controller.Get();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be("pong");
    }
}
