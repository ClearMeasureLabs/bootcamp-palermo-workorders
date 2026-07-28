using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public async Task Get_Should_ReturnOk_WithHelloWorldMessage()
    {
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        httpContext.RequestServices = services.BuildServiceProvider();

        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);

        await ok.ExecuteResultAsync(new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()));

        httpContext.Response.ContentType.ShouldNotBeNull();
        httpContext.Response.ContentType.ShouldContain("application/json");
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }
}
