using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.ServiceDefaults;
using ClearMeasure.Bootcamp.UI.Api;
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
        doc.RootElement.TryGetProperty("queryString", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("correlationId", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("timestampUtc", out _).ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("queryString", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("correlationId", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("timestampUtc", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_EchoQueryString_When_QueryParametersProvided()
    {
        var response = await _client!.GetAsync("/api/echo?foo=bar&baz=1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.QueryString.ShouldBe("?foo=bar&baz=1");
        payload.Query["foo"].ShouldBe("bar");
        payload.Query["baz"].ShouldBe("1");
    }

    [Test]
    public async Task Should_ReturnEmptyQueryString_When_NoQueryParameters()
    {
        var response = await _client!.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.QueryString.ShouldBe("");
        payload.Query.Count.ShouldBe(0);
    }

    [Test]
    public async Task Should_ReflectRequestMethodAndPath_When_GetIssued()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.UserAgent.ParseAdd("EchoTestAgent/1.0");

        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.Scheme.ShouldBe("http");
        payload.Host.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_ReturnCorrelationIdHeader_When_RequestProvidesHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Add(CorrelationIdConstants.HeaderName, "test-correlation-abc");

        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var headerValues).ShouldBeTrue();
        headerValues!.Single().ShouldBe("test-correlation-abc");

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.CorrelationId.ShouldBe("test-correlation-abc");
    }

    [Test]
    public async Task Should_GenerateCorrelationId_When_RequestOmitsHeader()
    {
        var response = await _client!.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var headerValues).ShouldBeTrue();
        var correlationId = headerValues!.Single();
        Guid.TryParse(correlationId, out _).ShouldBeTrue();

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.CorrelationId.ShouldBe(correlationId);
    }

    [Test]
    public async Task Should_IncludeSelectedHeaders_When_DiagnosticHeadersPresent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("EchoTestAgent/1.0");
        request.Headers.Add("X-Forwarded-For", "203.0.113.1");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret");
        request.Headers.Add("Cookie", "session=abc");

        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Headers["Accept"].ShouldBe("application/json");
        payload.Headers["User-Agent"].ShouldBe("EchoTestAgent/1.0");
        payload.Headers["X-Forwarded-For"].ShouldBe("203.0.113.1");
        payload.Headers.ContainsKey("Authorization").ShouldBeFalse();
        payload.Headers.ContainsKey("Cookie").ShouldBeFalse();
    }

    [Test]
    public async Task Should_RedactApiKeyInJson_When_ValidKeySupplied()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Headers.ContainsKey("X-Api-Key").ShouldBeTrue();
        payload.Headers["X-Api-Key"].ShouldBe("[present]");
        payload.Headers["X-Api-Key"].ShouldNotBe(ApiKeyProtectedWebApplicationFactory.TestApiKey);
    }

    [Test]
    public async Task Should_Return401WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/echo");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/echo");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200WithApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await client.GetAsync("/api/echo");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await client.GetAsync("/api/v1.0/echo");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
