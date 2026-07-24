using System.Reflection;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithJsonMessage()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var response = ok.Value.ShouldBeOfType<HelloResponse>();
        response.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_NotRequireAuthentication()
    {
        var method = typeof(HelloController).GetMethod(nameof(HelloController.Get));
        method.ShouldNotBeNull();
        method!.GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
    }
}
