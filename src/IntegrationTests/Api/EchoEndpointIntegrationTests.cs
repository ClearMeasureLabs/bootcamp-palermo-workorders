using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EchoEndpointIntegrationTests
{
    private DiagnosticsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DiagnosticsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        AssertEchoShape(doc.RootElement);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        AssertEchoShape(doc.RootElement);
    }

    [Test]
    public async Task Should_ReflectRequestMethod_When_Called()
    {
        var response = await _client!.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
    }

    [Test]
    public async Task Should_ReflectRequestPath_When_Called()
    {
        var response = await _client!.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Path.ShouldBe("/api/echo");
    }

    [Test]
    public async Task Should_ReflectQueryString_When_CalledWithQuery()
    {
        var response = await _client!.GetAsync("/api/echo?foo=bar&tag=a&tag=b");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.QueryString.ShouldBe("?foo=bar&tag=a&tag=b");
        payload.Query.ShouldContainKey("foo");
        payload.Query["foo"].ShouldBe(["bar"]);
        payload.Query.ShouldContainKey("tag");
        payload.Query["tag"].ShouldBe(["a", "b"]);
    }

    [Test]
    public async Task Should_ReflectHeaders_When_CalledWithHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("User-Agent", "EchoIntegrationTest/1.0");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers.Keys.ShouldContain(k => k.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
        payload.Headers.Keys.ShouldContain(k => k.Equals("Accept", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Should_ReflectRemoteIp_When_Called()
    {
        var response = await _client!.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("remoteIp", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void AssertEchoShape(JsonElement root)
    {
        root.TryGetProperty("method", out _).ShouldBeTrue();
        root.TryGetProperty("path", out _).ShouldBeTrue();
        root.TryGetProperty("queryString", out _).ShouldBeTrue();
        root.TryGetProperty("scheme", out _).ShouldBeTrue();
        root.TryGetProperty("host", out _).ShouldBeTrue();
        root.TryGetProperty("protocol", out _).ShouldBeTrue();
        root.TryGetProperty("remoteIp", out _).ShouldBeTrue();
        root.TryGetProperty("headers", out _).ShouldBeTrue();
        root.TryGetProperty("query", out _).ShouldBeTrue();
    }
}
