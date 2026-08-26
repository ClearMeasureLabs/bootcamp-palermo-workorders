using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task Should_Return200AndJson_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo?diag=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.QueryString.ShouldBe("?diag=1");
        payload.Headers.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Path.ShouldBe("/api/v1.0/echo");
        payload.Method.ShouldBe("GET");
    }

    [Test]
    public async Task Should_ReflectCustomHeader_When_Present()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("X-Client-Trace", "echo-it");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Headers.ShouldContainKey("X-Client-Trace");
        payload.Headers["X-Client-Trace"].ShouldBe("echo-it");
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
