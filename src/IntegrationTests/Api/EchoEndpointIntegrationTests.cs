using System.Net;
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
    public async Task Should_Return200AndJson_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await DeserializeAsync(response);
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await DeserializeAsync(response);
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/v1.0/echo");
    }

    [Test]
    public async Task Should_RoundTripQueryString_When_QueryProvided()
    {
        var response = await _client!.GetAsync("/api/echo?a=1&b=two");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeAsync(response);
        payload.Query["a"].ShouldBe("1");
        payload.Query["b"].ShouldBe("two");
    }

    [Test]
    public async Task Should_IncludeRequestMethod_When_GetCalled()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeAsync(response);
        payload.Method.ShouldBe("GET");
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

    private static async Task<EchoResponse> DeserializeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EchoResponse>(json, ConditionalGetEtag.JsonSerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize echo response.");
    }
}
