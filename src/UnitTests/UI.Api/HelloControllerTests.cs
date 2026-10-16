using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOkObjectResult()
    {
        var controller = new HelloController();

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
    }

    [Test]
    public void Get_Should_ReturnMessageHelloWorld()
    {
        var controller = new HelloController();

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var value = ok.Value!;
        var message = value.GetType().GetProperty("message")!.GetValue(value) as string;
        message.ShouldBe("Hello, World!");
    }
}
