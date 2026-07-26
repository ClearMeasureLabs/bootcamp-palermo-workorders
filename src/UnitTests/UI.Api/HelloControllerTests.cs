using System.Reflection;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOkObjectResult_With_HelloResponse()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<HelloResponse>();
        payload.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_ReturnApplicationJsonContentType()
    {
        var method = typeof(HelloController).GetMethod(nameof(HelloController.Get));
        method.ShouldNotBeNull();
        var contentTypes = method!.GetCustomAttributes<ProducesAttribute>(inherit: false)
            .SelectMany(attribute => attribute.ContentTypes)
            .ToList();
        contentTypes.ShouldContain("application/json");
    }
}
