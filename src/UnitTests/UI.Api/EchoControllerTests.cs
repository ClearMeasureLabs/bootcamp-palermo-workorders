using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    [Test]
    public void Should_ReturnOkStatus_When_GetEchoCalledWithValidRequest()
    {
        var controller = CreateController(new StubEchoHttpContext());

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
    }

    [Test]
    public void Should_IncludeRequestMethod_InResponse()
    {
        var stubContext = new StubEchoHttpContext { Method = "GET" };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Method.ShouldBe("GET");
    }

    [Test]
    public void Should_IncludeRequestPath_InResponse()
    {
        var stubContext = new StubEchoHttpContext { Path = "/api/echo" };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Path.ShouldBe("/api/echo");
    }

    [Test]
    public void Should_IncludeQueryString_InResponse()
    {
        var stubContext = new StubEchoHttpContext { QueryString = "?foo=bar" };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.QueryString.ShouldBe("?foo=bar");
    }

    [Test]
    public void Should_IncludeSchemeAndHost_InResponse()
    {
        var stubContext = new StubEchoHttpContext
        {
            Scheme = "https",
            Host = "localhost:7174"
        };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
    }

    [Test]
    public void Should_IncludeProtocolVersion_InResponse()
    {
        var stubContext = new StubEchoHttpContext { Protocol = "HTTP/2" };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Protocol.ShouldBe("HTTP/2");
    }

    [Test]
    public void Should_IncludeRemoteIpAddress_InResponse()
    {
        var stubContext = new StubEchoHttpContext { RemoteIpAddress = "192.168.1.10" };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.RemoteIpAddress.ShouldBe("192.168.1.10");
    }

    [Test]
    public void Should_IncludeHeadersDictionary_InResponse()
    {
        var stubContext = new StubEchoHttpContext();
        stubContext.Headers["User-Agent"] = "TestAgent/1.0";
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Headers.TryGetValue("User-Agent", out var value).ShouldBeTrue();
        value.ShouldBe("TestAgent/1.0");
    }

    [Test]
    public void Should_RedactAuthorizationHeader_Value()
    {
        var stubContext = new StubEchoHttpContext();
        stubContext.Headers["Authorization"] = "Bearer secret-token";
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Headers.TryGetValue("Authorization", out var value).ShouldBeTrue();
        value.ShouldBe(EchoHeaderRedaction.RedactedValue);
    }

    [Test]
    public void Should_RedactApiKeyHeader_Value()
    {
        var stubContext = new StubEchoHttpContext();
        stubContext.Headers[ApiKeyConstants.HeaderName] = "super-secret-key";
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var value).ShouldBeTrue();
        value.ShouldBe(EchoHeaderRedaction.RedactedValue);
    }

    [Test]
    public void Should_RedactCookieHeader_Value()
    {
        var stubContext = new StubEchoHttpContext();
        stubContext.Headers["Cookie"] = "session=abc123";
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Headers.TryGetValue("Cookie", out var value).ShouldBeTrue();
        value.ShouldBe(EchoHeaderRedaction.RedactedValue);
    }

    [Test]
    public void Should_IncludeCustomHeaders_InResponse()
    {
        var stubContext = new StubEchoHttpContext();
        stubContext.Headers["Custom-Header"] = "visible-value";
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Headers.TryGetValue("Custom-Header", out var value).ShouldBeTrue();
        value.ShouldBe("visible-value");
    }

    [Test]
    public void Should_ParseQueryParameters_IntoMap_When_QueryPresent()
    {
        var stubContext = new StubEchoHttpContext
        {
            QueryString = "?name=Alice&role=admin"
        };
        var controller = CreateController(stubContext);

        var payload = GetPayload(controller.Get());

        payload.Query.ShouldNotBeNull();
        payload.Query!["name"].ShouldBe("Alice");
        payload.Query["role"].ShouldBe("admin");
    }

    [Test]
    public void Should_ReturnNullQuery_When_QueryAbsent()
    {
        var controller = CreateController(new StubEchoHttpContext());

        var payload = GetPayload(controller.Get());

        payload.Query.ShouldBeNull();
    }

    private static EchoController CreateController(StubEchoHttpContext stubContext)
    {
        return new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = stubContext.HttpContext }
        };
    }

    private static EchoResponse GetPayload(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }

    private sealed class StubEchoHttpContext
    {
        private readonly DefaultHttpContext _httpContext = new();

        public StubEchoHttpContext()
        {
            _httpContext.Request.Method = "GET";
            _httpContext.Request.Path = "/api/echo";
            _httpContext.Request.Scheme = "http";
            _httpContext.Request.Host = new HostString("localhost");
            _httpContext.Request.Protocol = "HTTP/1.1";
        }

        public DefaultHttpContext HttpContext => _httpContext;

        public string Method
        {
            set => _httpContext.Request.Method = value;
        }

        public string Path
        {
            set => _httpContext.Request.Path = value;
        }

        public string QueryString
        {
            set => _httpContext.Request.QueryString = new QueryString(value);
        }

        public string Scheme
        {
            set => _httpContext.Request.Scheme = value;
        }

        public string Host
        {
            set => _httpContext.Request.Host = new HostString(value);
        }

        public string Protocol
        {
            set => _httpContext.Request.Protocol = value;
        }

        public string RemoteIpAddress
        {
            set => _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(value);
        }

        public IHeaderDictionary Headers => _httpContext.Request.Headers;
    }
}
