using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.ServiceDefaults;
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
    public async Task Should_Return200AndJson_When_GetEchoUnversioned()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo?foo=bar&foo=baz");
        request.Headers.Add("User-Agent", "echo-test");
        request.Headers.Add(CorrelationIdConstants.HeaderName, "my-trace-1");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        response.Headers.GetValues(CorrelationIdConstants.HeaderName).Single().ShouldBe("my-trace-1");

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.QueryString.ShouldNotBeNull();
        payload.QueryString!.ShouldContain("foo=bar");
        payload.Query["foo"].ShouldBe(["bar", "baz"]);
        payload.Headers.ContainsKey("User-Agent").ShouldBeTrue();
        payload.CorrelationId.ShouldBe("my-trace-1");
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEchoVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo?debug=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/v1.0/echo");
        payload.Query["debug"].ShouldBe(["1"]);
    }

    [Test]
    public async Task Should_RedactSensitiveHeaders_When_SentOnRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Add("Authorization", "Bearer secret-token");
        request.Headers.Add("Cookie", "session=abc");
        request.Headers.Add("X-Api-Key", "super-secret");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("secret-token");
        body.ShouldNotContain("session=abc");
        body.ShouldNotContain("super-secret");

        var payload = JsonSerializer.Deserialize<EchoResponse>(
            body,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers["Authorization"].ShouldBe([EchoRequestReflectionBuilder.RedactedHeaderValue]);
        payload.Headers["Cookie"].ShouldBe([EchoRequestReflectionBuilder.RedactedHeaderValue]);
        payload.Headers["X-Api-Key"].ShouldBe([EchoRequestReflectionBuilder.RedactedHeaderValue]);
    }

    [Test]
    public async Task Should_ReturnCorrelationIdHeader_When_RequestHasNoCorrelationHeader()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var headerValues).ShouldBeTrue();
        var correlationId = headerValues!.Single();
        correlationId.Length.ShouldBeGreaterThan(0);
        Guid.TryParse(correlationId, out _).ShouldBeTrue();

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.CorrelationId.ShouldBe(correlationId);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedMediaType = unversioned.Content.Headers.ContentType?.MediaType;
        unversionedMediaType.ShouldNotBeNull();
        unversionedMediaType!.ShouldContain("application/json");

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedMediaType = versioned.Content.Headers.ContentType?.MediaType;
        versionedMediaType.ShouldNotBeNull();
        versionedMediaType!.ShouldContain("application/json");
    }
}
