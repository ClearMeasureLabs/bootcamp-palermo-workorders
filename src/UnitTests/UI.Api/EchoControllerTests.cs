using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    [Test]
    public void Get_Should_ReflectMethodPathQueryAndHeaders()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:7174");
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.QueryString = new QueryString("?foo=bar&x=1");
        httpContext.Request.Protocol = "HTTP/2";
        httpContext.Request.Headers["X-Debug"] = "trace-1";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<EchoResponse>();
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.QueryString.ShouldBe("?foo=bar&x=1");
        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
        payload.Protocol.ShouldBe("HTTP/2");
        payload.RemoteIpAddress.ShouldBe("127.0.0.1");
        payload.Headers["X-Debug"].ShouldBe("trace-1");
    }

    [Test]
    public void Get_Should_RedactSensitiveHeaders()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.Headers["Authorization"] = "Bearer secret-token";
        httpContext.Request.Headers["X-Api-Key"] = "api-key-value";
        httpContext.Request.Headers["Cookie"] = "session=abc";
        httpContext.Request.Headers["Accept"] = "application/json";

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<EchoResponse>();
        payload.Headers["Authorization"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["X-Api-Key"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["Cookie"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["Accept"].ShouldBe("application/json");
    }
}
