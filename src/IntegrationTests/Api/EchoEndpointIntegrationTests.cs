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
    public async Task Should_Return200WithJsonAndReflectRequest_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeEchoResponse(response);
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
    }

    [Test]
    public async Task Should_Return200WithJsonAndReflectRequest_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeEchoResponse(response);
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/v1.0/echo");
    }

    [Test]
    public async Task Should_RoundTripQueryString_MultipleParams()
    {
        var response = await _client!.GetAsync("/api/echo?key1=value1&key2=value2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeEchoResponse(response);
        payload.QueryString.ShouldBe("?key1=value1&key2=value2");
        payload.Query["key1"].ShouldBe(["value1"]);
        payload.Query["key2"].ShouldBe(["value2"]);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unversioned.Content.ReadAsStringAsync()).ShouldContain("\"method\":\"GET\"");

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await versioned.Content.ReadAsStringAsync()).ShouldContain("\"method\":\"GET\"");
    }

    private static async Task<EchoResponse> DeserializeEchoResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<EchoResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
