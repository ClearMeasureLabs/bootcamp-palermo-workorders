using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class IdempotencyMiddlewareTests
{
    [TestCase("GET", "/api/items", false)]
    [TestCase("POST", "/api/items", true)]
    [TestCase("PUT", "/api/items/1", true)]
    [TestCase("POST", "/api/blazor-wasm-single-api", true)]
    [TestCase("POST", "/home", false)]
    public void ShouldInspect_ReturnsExpected(string method, string path, bool expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Method = method;
        request.Path = path;

        IdempotencyMiddleware.ShouldInspect(request).ShouldBe(expected);
    }

    [Test]
    public void TryReadIdempotencyKey_ReturnsNull_WhenHeaderMissing()
    {
        var request = new DefaultHttpContext().Request;
        IdempotencyMiddleware.TryReadIdempotencyKey(request).ShouldBeNull();
    }

    [Test]
    public void TryReadIdempotencyKey_ReturnsTrimmedValue_WhenHeaderPresent()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers["Idempotency-Key"] = "  abc-123  ";
        IdempotencyMiddleware.TryReadIdempotencyKey(request).ShouldBe("abc-123");
    }

    [Test]
    public async Task BuildCompositeKeyAsync_IncludesBodyHash()
    {
        var bodyBytes = "payload"u8.ToArray();
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "POST",
                Path = "/api/test",
                Body = new MemoryStream(bodyBytes),
                ContentLength = bodyBytes.Length
            }
        };

        var key1 = await IdempotencyMiddleware.BuildCompositeKeyAsync(context.Request, "k1", CancellationToken.None);
        context.Request.Body = new MemoryStream(bodyBytes);
        var key2 = await IdempotencyMiddleware.BuildCompositeKeyAsync(context.Request, "k1", CancellationToken.None);
        key1.ShouldBe(key2);
        key1.ShouldContain("k1");
    }
}
