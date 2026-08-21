using System.Text.Json;
using ClearMeasure.Bootcamp.ServiceDefaults;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Get_Should_ReturnJson_WithRequestReflection_When_QueryAndHeadersPresent()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 7, 12, 15, 0, 0, TimeSpan.Zero));
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost", 7174);
        context.Request.Path = "/api/echo";
        context.Request.PathBase = "/app";
        context.Request.QueryString = new QueryString("?a=1&b=2");
        context.Request.Headers.Accept = "application/json";
        context.Request.Headers.UserAgent = "EchoTest/1.0";

        var controller = new EchoController(clock)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
        payload.Path.ShouldBe("/api/echo");
        payload.PathBase.ShouldBe("/app");
        payload.QueryString.ShouldBe("?a=1&b=2");
        payload.Query["a"].ShouldBe("1");
        payload.Query["b"].ShouldBe("2");
        payload.Headers["Accept"].ShouldBe("application/json");
        payload.Headers["User-Agent"].ShouldBe("EchoTest/1.0");
    }

    [Test]
    public void Get_Should_RedactSensitiveHeaders_When_AuthorizationOrApiKeyPresent()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 7, 12, 15, 0, 0, TimeSpan.Zero));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer secret";
        context.Request.Headers.Cookie = "session=abc";
        context.Request.Headers["X-Api-Key"] = "my-secret-key";
        context.Request.Headers.UserAgent = "EchoTest/1.0";

        var controller = new EchoController(clock)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers.ContainsKey("Authorization").ShouldBeFalse();
        payload.Headers.ContainsKey("Cookie").ShouldBeFalse();
        payload.Headers["X-Api-Key"].ShouldBe("[present]");
        payload.Headers["User-Agent"].ShouldBe("EchoTest/1.0");
    }

    [Test]
    public void Get_Should_UseCorrelationIdFromHttpContextItems_When_Set()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 7, 12, 15, 0, 0, TimeSpan.Zero));
        var context = new DefaultHttpContext();
        context.Items[CorrelationIdConstants.HttpContextItemKey] = "test-correlation-abc";

        var controller = new EchoController(clock)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.CorrelationId.ShouldBe("test-correlation-abc");
    }

    [Test]
    public void Get_Should_ReturnNullCorrelationId_When_ItemNotSet()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 7, 12, 15, 0, 0, TimeSpan.Zero));
        var context = new DefaultHttpContext();

        var controller = new EchoController(clock)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.CorrelationId.ShouldBeNull();
    }

    [Test]
    public void Get_Should_UseInjectedTimeProvider_ForTimestampUtc()
    {
        var expected = new DateTimeOffset(2026, 7, 12, 12, 30, 45, TimeSpan.Zero);
        var clock = new FixedUtcTimeProvider(expected);
        var context = new DefaultHttpContext();

        var controller = new EchoController(clock)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.TimestampUtc.ShouldBe(expected);
    }
}
