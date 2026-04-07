using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class EchoEndpointWebTests
{
    [Test]
    public async Task Should_Return200AndJson_When_GetEcho()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        response.Headers.TryGetValues("Cache-Control", out var cacheControl).ShouldBeTrue();
        string.Join(", ", cacheControl!).ShouldContain("no-store");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("method").GetString().ShouldBe("GET");
        doc.RootElement.GetProperty("scheme").GetString().ShouldNotBeNullOrEmpty();
        doc.RootElement.GetProperty("host").GetString().ShouldNotBeNullOrEmpty();
        var path = doc.RootElement.GetProperty("path").GetString();
        path.ShouldNotBeNull();
        path!.ShouldContain("echo");
        doc.RootElement.TryGetProperty("pathBase", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("queryString", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("headers", out var headers).ShouldBeTrue();
        headers.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Test]
    public async Task Should_ReflectQueryString_When_RequestHasQueryParameters()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo?debug=1&foo=bar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var qs = doc.RootElement.GetProperty("queryString").GetString();
        qs.ShouldNotBeNull();
        qs!.ShouldContain("debug=1");
        qs.ShouldContain("foo=bar");
    }

    [Test]
    public async Task Should_ReflectCustomHeaders_And_RedactSensitiveHeaders()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("X-Test-Echo", "abc");
        request.Headers.TryAddWithoutValidation("Cookie", "session=secret");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer secret-token");

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var headers = doc.RootElement.GetProperty("headers");
        headers.GetProperty("X-Test-Echo").GetString().ShouldBe("abc");
        headers.GetProperty("Cookie").GetString().ShouldBe("***");
        headers.GetProperty("Authorization").GetString().ShouldBe("***");
    }

    [Test]
    public async Task Should_RedactApiKeyHeader_When_PresentOnRequest()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("headers").GetProperty(ApiKeyConstants.HeaderName).GetString().ShouldBe("***");
    }

    [Test]
    public async Task Should_Return200ForVersionedPath_When_ApiVersioningEnabled()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var legacy = await client.GetAsync("/api/echo");
        var v1 = await client.GetAsync("/api/v1.0/echo");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var legacyDoc = JsonDocument.Parse(await legacy.Content.ReadAsStringAsync());
        using var v1Doc = JsonDocument.Parse(await v1.Content.ReadAsStringAsync());
        legacyDoc.RootElement.GetProperty("method").GetString().ShouldBe(v1Doc.RootElement.GetProperty("method").GetString());
        var legacyPath = legacyDoc.RootElement.GetProperty("path").GetString();
        var v1Path = v1Doc.RootElement.GetProperty("path").GetString();
        legacyPath.ShouldNotBeNull();
        v1Path.ShouldNotBeNull();
        legacyPath!.ShouldContain("echo");
        v1Path!.ShouldContain("echo");
    }
}
