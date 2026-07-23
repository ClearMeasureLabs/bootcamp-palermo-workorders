using ClearMeasure.Bootcamp.UI.Server.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class StatusControllerTests
{
    [Fact]
    public void Get_ShouldReturnOkWithStatusOk()
    {
        // Arrange
        var controller = new StatusController();

        // Act
        var result = controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(new { status = "ok" });
    }
}
