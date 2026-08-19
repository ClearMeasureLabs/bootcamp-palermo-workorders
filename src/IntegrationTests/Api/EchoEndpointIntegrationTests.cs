using System.Net;
using System.Net.Http.Headers;
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

    private static async Task<EchoResponse> GetEchoResponseAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            json,
            ConditionalGetEtag.JsonSerializerOptions);
        return payload.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEchoUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo");
        var payload = await GetEchoResponseAsync(response);

        payload.Method.ShouldBe("GET");
        payload.Path.ShouldNotBeNullOrWhiteSpace();
        payload.Query.ShouldNotBeNull();
        payload.Headers.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEchoVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");
        var payload = await GetEchoResponseAsync(response);

        payload.Method.ShouldBe("GET");
        payload.Path.ShouldNotBeNullOrWhiteSpace();
        payload.Query.ShouldNotBeNull();
        payload.Headers.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_ReflectQueryStringParameters_When_GetEchoWithQuery()
    {
        var response = await _client!.GetAsync("/api/echo?key1=val1&key2=val2a&key2=val2b");
        var payload = await GetEchoResponseAsync(response);

        payload.Query["key1"].ShouldBe(["val1"]);
        payload.Query["key2"].ShouldBe(["val2a", "val2b"]);
    }

    [Test]
    public async Task Should_ReflectCustomHeaders_When_GetEchoWithHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        request.Headers.Add("X-Correlation-Id", "abc-123");
        request.Headers.UserAgent.ParseAdd("TestClient/1.0");

        var response = await _client!.SendAsync(request);
        var payload = await GetEchoResponseAsync(response);

        payload.Headers["Accept"].ShouldBe("application/xml");
        payload.Headers["X-Correlation-Id"].ShouldBe("abc-123");
        payload.Headers["User-Agent"].ShouldBe("TestClient/1.0");
    }

    [Test]
    public async Task Should_OmitAuthorizationHeader_When_GetEchoWithAuthorizationHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token123");

        var response = await _client!.SendAsync(request);
        var payload = await GetEchoResponseAsync(response);

        payload.Headers.ContainsKey("Authorization").ShouldBeFalse();
    }

    [Test]
    public async Task Should_OmitCookieHeader_When_GetEchoWithCookie()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Add("Cookie", "session=xyz");

        var response = await _client!.SendAsync(request);
        var payload = await GetEchoResponseAsync(response);

        payload.Headers.ContainsKey("Cookie").ShouldBeFalse();
    }

    [Test]
    public async Task Should_OmitXApiKeyHeader_When_GetEchoWithXApiKey()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Add("X-API-Key", "secret123");

        var response = await _client!.SendAsync(request);
        var payload = await GetEchoResponseAsync(response);

        payload.Headers.ContainsKey("X-API-Key").ShouldBeFalse();
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        var unversionedPayload = await GetEchoResponseAsync(unversioned);
        unversionedPayload.Method.ShouldBe("GET");

        var versioned = await client.GetAsync("/api/v1.0/echo");
        var versionedPayload = await GetEchoResponseAsync(versioned);
        versionedPayload.Method.ShouldBe("GET");
    }
}
