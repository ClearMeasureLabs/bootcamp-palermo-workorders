using ClearMeasure.Bootcamp.ServiceDefaults;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoRequestReflectionBuilderTests
{
    [Test]
    public void Should_BuildEchoResponse_When_QueryAndHeadersPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.PathBase = "/app";
        context.Request.QueryString = new QueryString("?a=1&b=2&a=3");
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com", 443);
        context.Request.Headers["User-Agent"] = "echo-test-agent";
        context.Request.Headers["Accept"] = "application/json";

        var response = EchoRequestReflectionBuilder.Build(context);

        response.Method.ShouldBe("GET");
        response.Path.ShouldBe("/api/echo");
        response.PathBase.ShouldBe("/app");
        response.QueryString.ShouldBe("?a=1&b=2&a=3");
        response.Query["a"].ShouldBe(["1", "3"]);
        response.Query["b"].ShouldBe(["2"]);
        response.Scheme.ShouldBe("https");
        response.Host.ShouldBe("example.com:443");
        response.Headers["User-Agent"].ShouldBe(["echo-test-agent"]);
        response.Headers["Accept"].ShouldBe(["application/json"]);
    }

    [Test]
    public void Should_RedactSensitiveHeaders_When_AuthorizationCookieOrApiKeyPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer secret-token";
        context.Request.Headers["Cookie"] = "session=abc";
        context.Request.Headers["X-Api-Key"] = "super-secret";
        context.Request.Headers["Accept"] = "application/json";
        context.Request.Headers["User-Agent"] = "safe-agent";

        var response = EchoRequestReflectionBuilder.Build(context);

        response.Headers["Authorization"].ShouldBe([EchoRequestReflectionBuilder.RedactedHeaderValue]);
        response.Headers["Cookie"].ShouldBe([EchoRequestReflectionBuilder.RedactedHeaderValue]);
        response.Headers["X-Api-Key"].ShouldBe([EchoRequestReflectionBuilder.RedactedHeaderValue]);
        response.Headers["Accept"].ShouldBe(["application/json"]);
        response.Headers["User-Agent"].ShouldBe(["safe-agent"]);
    }

    [Test]
    public void Should_UseCorrelationIdFromHttpContextItems_When_MiddlewareSet()
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationIdConstants.HttpContextItemKey] = "from-middleware";
        context.Request.Headers[CorrelationIdConstants.HeaderName] = "from-header";

        var response = EchoRequestReflectionBuilder.Build(context);

        response.CorrelationId.ShouldBe("from-middleware");
    }

    [Test]
    public void Should_FallbackToCorrelationHeader_When_ItemsNotSet()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdConstants.HeaderName] = "from-header";

        var response = EchoRequestReflectionBuilder.Build(context);

        response.CorrelationId.ShouldBe("from-header");
    }
}
