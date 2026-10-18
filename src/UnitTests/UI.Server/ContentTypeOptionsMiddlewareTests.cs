using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ContentTypeOptionsMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_Should_SetNosniff_When_HeaderMissing()
    {
        var context = new DefaultHttpContext();
        var middleware = new ContentTypeOptionsMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[ContentTypeOptionsMiddleware.HeaderName]
            .ToString()
            .ShouldBe(ContentTypeOptionsMiddleware.HeaderValue);
    }

    [Test]
    public async Task InvokeAsync_Should_PreserveExistingValue_When_HeaderAlreadySet()
    {
        var context = new DefaultHttpContext();
        context.Response.Headers[ContentTypeOptionsMiddleware.HeaderName] = "existing";
        var middleware = new ContentTypeOptionsMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[ContentTypeOptionsMiddleware.HeaderName]
            .ToString()
            .ShouldBe("existing");
    }

    [Test]
    public void ApplyHeader_Should_SetNosniff_When_HeaderMissing()
    {
        var headers = new HeaderDictionary();

        ContentTypeOptionsMiddleware.ApplyHeader(headers);

        headers[ContentTypeOptionsMiddleware.HeaderName]
            .ToString()
            .ShouldBe(ContentTypeOptionsMiddleware.HeaderValue);
    }
}
