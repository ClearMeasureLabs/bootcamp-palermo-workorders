using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEchoVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_ReflectRequestMethod_When_GetEcho()
    {
        var response = await _client!.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
    }

    [Test]
    public async Task Should_ReflectQueryParameters_When_GetEchoWithQuery()
    {
        var response = await _client!.GetAsync("/api/echo?key1=val1&key2=val2");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Query["key1"].ShouldBe("val1");
        payload.Query["key2"].ShouldBe("val2");
        payload.QueryString.ShouldBe("?key1=val1&key2=val2");
    }

    [Test]
    public async Task Should_ReflectSelectedHeaders_When_GetEcho()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("User-Agent", "EchoIntegrationTest/1.0");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer secret-token");
        request.Headers.TryAddWithoutValidation("X-Custom-Header", "visible");

        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers.ContainsKey("Authorization").ShouldBeFalse();
        payload.Headers.ContainsKey("Host").ShouldBeTrue();
        payload.Headers["User-Agent"].ShouldBe("EchoIntegrationTest/1.0");
        payload.Headers["X-Custom-Header"].ShouldBe("visible");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedPayload = await unversioned.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        unversionedPayload.ShouldNotBeNull();
        unversionedPayload!.Method.ShouldBe("GET");

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedPayload = await versioned.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        versionedPayload.ShouldNotBeNull();
        versionedPayload!.Method.ShouldBe("GET");
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndEchoProtected()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/diagnostics");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/diagnostics");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/diagnostics");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/diagnostics");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
