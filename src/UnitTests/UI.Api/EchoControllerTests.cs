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
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Scheme = "https",
                Host = new HostString("localhost:7174"),
                Path = "/api/echo",
                QueryString = new QueryString("?foo=bar&x=1"),
                Protocol = "HTTP/2"
            },
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1")
            }
        };
        httpContext.Request.Headers["X-Debug"] = "trace-1";

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
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/api/echo"
            }
        };
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

    [Test]
    public void BuildEchoResponse_NonSensitiveHeader_IsPassedThrough()
    {
        var httpContext = new DefaultHttpContext { Request = { Method = "GET", Path = "/api/echo" } };
        httpContext.Request.Headers["X-Custom-Trace"] = "trace-value-42";

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        payload.Headers["X-Custom-Trace"].ShouldBe("trace-value-42");
    }

    [Test]
    public void BuildEchoResponse_IPv4MappedAddress_IsFlattenedToIPv4()
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Method = "GET", Path = "/api/echo" },
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("::ffff:127.0.0.1") }
        };

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        payload.RemoteIpAddress.ShouldBe("127.0.0.1");
    }

    [Test]
    public void BuildEchoResponse_EmptyQueryString_IsEmptyString()
    {
        var httpContext = new DefaultHttpContext { Request = { Method = "GET", Path = "/api/echo" } };

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        payload.QueryString.ShouldBe(string.Empty);
    }
}
