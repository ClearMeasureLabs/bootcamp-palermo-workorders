using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class StatusControllerTests
{
    [Test]
    public void Should_Get_ReturnOkWithStatusOk()
    {
        var controller = new StatusController();

        var result = controller.Get();

        result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.ShouldNotBeNull();
        var statusProperty = value.GetType().GetProperty("status");
        statusProperty.ShouldNotBeNull();
        statusProperty.GetValue(value).ShouldBe("ok");
    }
}
