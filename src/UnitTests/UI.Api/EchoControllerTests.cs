using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    private static EchoController CreateController(HttpContext context) =>
        new() { ControllerContext = new ControllerContext { HttpContext = context } };

    private static EchoResponse Deserialize(ContentResult content)
    {
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        return payload.ShouldNotBeNull();
    }

    [Test]
    public void Get_Should_ReturnJson200_When_CalledWithMethod()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        Deserialize(content).Method.ShouldBe("GET");
    }

    [Test]
    public void Get_Should_ReflectPathAndPathBase_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = new PathString("/app");
        context.Request.Path = new PathString("/api/echo");

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        Deserialize(content).Path.ShouldBe("/app/api/echo");
    }

    [Test]
    public void Get_Should_ReflectQueryStringKeyValues_When_CalledWithQuery()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?id=1&id=2&name=test");

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var query = Deserialize(content).Query;
        query["id"].ShouldBe(["1", "2"]);
        query["name"].ShouldBe(["test"]);
    }

    [Test]
    public void Get_Should_ReflectAllowlistedHeaders_When_CalledWithHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept"] = "application/json";
        context.Request.Headers["User-Agent"] = "TestAgent/1.0";
        context.Request.Headers["Host"] = "localhost";
        context.Request.Headers["X-Correlation-Id"] = "corr-123";
        context.Request.Headers["X-Forwarded-For"] = "10.0.0.1";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var headers = Deserialize(content).Headers;
        headers["Accept"].ShouldBe("application/json");
        headers["User-Agent"].ShouldBe("TestAgent/1.0");
        headers["Host"].ShouldBe("localhost");
        headers["X-Correlation-Id"].ShouldBe("corr-123");
        headers["X-Forwarded-For"].ShouldBe("10.0.0.1");
        headers["X-Forwarded-Proto"].ShouldBe("https");
    }

    [Test]
    public void Get_Should_OmitSensitiveHeaders_When_CalledWithAuthorizationOrCookieOrXApiKey()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer secret-token";
        context.Request.Headers["Cookie"] = "session=abc123";
        context.Request.Headers["X-API-Key"] = "api-key-secret";
        context.Request.Headers["Accept"] = "application/json";

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var headers = Deserialize(content).Headers;
        headers.ContainsKey("Authorization").ShouldBeFalse();
        headers.ContainsKey("Cookie").ShouldBeFalse();
        headers.ContainsKey("X-API-Key").ShouldBeFalse();
        headers["Accept"].ShouldBe("application/json");
    }

    [Test]
    public void Get_Should_ReturnContentTypeJson_When_Called()
    {
        var context = new DefaultHttpContext();

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldBe("application/json; charset=utf-8");
    }
}
