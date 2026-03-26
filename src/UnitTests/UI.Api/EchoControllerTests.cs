using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    [Test]
    public void Should_Return200_AndReflectMethodPathAndQuery_When_GetEcho()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.QueryString = new QueryString("?foo=bar&baz=1&foo=last");

        var controller = CreateController(httpContext);

        var result = controller.Get();

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var body = ok.Value.ShouldBeOfType<EchoResponse>();
        body.Method.ShouldBe("GET");
        body.Path.ShouldBe("/api/echo");
        body.QueryParameters.Count.ShouldBe(2);
        body.QueryParameters["foo"].ShouldBe("last");
        body.QueryParameters["baz"].ShouldBe("1");
    }

    [Test]
    public void Should_IncludeOnlyAllowlistedHeaders_When_SafeHeadersPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.Headers[HeaderNames.UserAgent] = "TestAgent/1";
        httpContext.Request.Headers[HeaderNames.Accept] = "application/json";
        httpContext.Request.Headers[HeaderNames.Host] = "example.test";

        var controller = CreateController(httpContext);

        var result = controller.Get();

        var body = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        body.Headers.Count.ShouldBe(3);
        body.Headers[HeaderNames.UserAgent].ShouldBe("TestAgent/1");
        body.Headers[HeaderNames.Accept].ShouldBe("application/json");
        body.Headers[HeaderNames.Host].ShouldBe("example.test");
    }

    [Test]
    public void Should_ExcludeAuthorizationAndCookie_When_SensitiveHeadersPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.Headers[HeaderNames.UserAgent] = "ua";
        httpContext.Request.Headers[HeaderNames.Authorization] = "Bearer secret";
        httpContext.Request.Headers[HeaderNames.Cookie] = "session=abc";

        var controller = CreateController(httpContext);

        var result = controller.Get();

        var body = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        body.Headers.ContainsKey(HeaderNames.Authorization).ShouldBeFalse();
        body.Headers.ContainsKey(HeaderNames.Cookie).ShouldBeFalse();
        body.Headers[HeaderNames.UserAgent].ShouldBe("ua");
    }

    [Test]
    public void Should_ReturnRemoteIpAndUtcTimestamp_When_ConnectionAndClockKnown()
    {
        var fixedTime = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.Zero);
        var stubTimeProvider = new StubTimeProvider(fixedTime);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");

        var controller = new EchoController(stubTimeProvider) { ControllerContext = new ControllerContext { HttpContext = httpContext } };

        var result = controller.Get();

        var body = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        body.RemoteIp.ShouldBe("192.0.2.1");
        body.Timestamp.ShouldBe(fixedTime.UtcDateTime);
        body.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void Should_SerializeResponseWithCamelCaseJson_When_DefaultAspNetCoreNaming()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";

        var controller = CreateController(httpContext);
        var body = controller.Get().Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();

        var json = JsonSerializer.Serialize(body);
        json.ShouldContain("\"method\"");
        json.ShouldContain("\"queryParameters\"");
        json.ShouldContain("\"remoteIp\"");
    }

    private static EchoController CreateController(HttpContext httpContext)
    {
        return new EchoController(TimeProvider.System)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
