using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class EchoWebTests
{
    [Test]
    public async Task Should_ReturnJsonEcho_When_UnversionedPathWithQuery()
    {
        await using var factory = new CorsEnabledApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/echo?a=1&b=two&a=last");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.GetProperty("path").GetString().ShouldBe("/api/echo");
        root.GetProperty("queryString").GetString().ShouldBe("?a=1&b=two&a=last");
        root.GetProperty("query").GetProperty("a").GetString().ShouldBe("last");
        root.GetProperty("query").GetProperty("b").GetString().ShouldBe("two");
        root.GetProperty("method").GetString().ShouldBe("GET");
    }

    [Test]
    public async Task Should_ReturnJsonEcho_When_VersionedPath()
    {
        await using var factory = new CorsEnabledApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1.0/echo?q=test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.GetProperty("path").GetString().ShouldBe("/api/v1.0/echo");
        root.GetProperty("query").GetProperty("q").GetString().ShouldBe("test");
    }

    [Test]
    public async Task Should_NotIncludeSensitiveHeaders_InEchoJson()
    {
        await using var factory = new CorsEnabledApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer secret");
        request.Headers.TryAddWithoutValidation("X-Api-Key", "super-secret");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var headers = doc.RootElement.GetProperty("headers");

        headers.TryGetProperty("Authorization", out _).ShouldBeFalse();
        headers.TryGetProperty("X-Api-Key", out _).ShouldBeFalse();
    }
}
