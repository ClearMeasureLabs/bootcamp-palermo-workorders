using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
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
    public async Task Should_Return200AndJson_When_GetEchoUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("method", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("path", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("query", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clientIp", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEchoVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("method", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("path", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("query", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clientIp", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReflectQueryAndHeaders_When_RequestIncludesCustomValues()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo?tag=one&tag=two");
        request.Headers.Add("X-Custom-Header", "alpha");
        request.Headers.Add("X-Multi-Header", new[] { "first", "second" });

        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Query["tag"].ShouldBe(["one", "two"]);
        payload.Headers["X-Custom-Header"].ShouldBe(["alpha"]);
        payload.Headers["X-Multi-Header"].ShouldBe(["first", "second"]);
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
}
