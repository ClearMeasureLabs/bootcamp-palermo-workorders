using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
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
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.TryGetProperty("method", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("path", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("query", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_RoundTripQueryString_When_QueryProvided()
    {
        var response = await _client!.GetAsync("/api/echo?a=1&b=two");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Query["a"].ShouldBe("1");
        payload.Query["b"].ShouldBe("two");
    }

    [Test]
    public async Task Should_IncludeRequestMethod_When_GetCalled()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
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

    [Test]
    public async Task Should_EnforceApiKeyWhen_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var withoutKey = await client.GetAsync("/api/diagnostics");
        withoutKey.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var echoWithoutKey = await client.GetAsync("/api/echo");
        echoWithoutKey.StatusCode.ShouldBe(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var echoWithKey = await client.GetAsync("/api/echo");
        echoWithKey.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_IncludeCustomHeader_When_XHeaderSent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("X-Debug", "trace-1");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers["X-Debug"].ShouldBe("trace-1");
    }
}
