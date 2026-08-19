using System.Net;
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

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.TryGetProperty("method", out _).ShouldBeTrue();
        root.TryGetProperty("path", out _).ShouldBeTrue();
        root.TryGetProperty("query", out _).ShouldBeTrue();
        root.TryGetProperty("headers", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("method", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_RoundTripQueryString_When_QueryProvided()
    {
        var response = await _client!.GetAsync("/api/echo?a=1&b=two");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var query = document.RootElement.GetProperty("query");
        query.GetProperty("a").GetString().ShouldBe("1");
        query.GetProperty("b").GetString().ShouldBe("two");
    }

    [Test]
    public async Task Should_IncludeRequestMethod_When_GetCalled()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("method").GetString().ShouldBe("GET");
    }

    [Test]
    public async Task Should_IncludeCustomHeader_When_HeaderProvided()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Add("X-Test", "debug-value");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var headers = document.RootElement.GetProperty("headers");
        headers.GetProperty("X-Test").GetString().ShouldBe("debug-value");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_AnonymousProbe()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
