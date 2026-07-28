using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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
    public void Get_Should_ReturnOk_WithGreeting()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var payload = JsonSerializer.Deserialize<HelloResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_AllowAnonymousAccess()
    {
        var method = typeof(HelloController).GetMethod(nameof(HelloController.Get));
        method.ShouldNotBeNull();
        method!.GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
    }
}
