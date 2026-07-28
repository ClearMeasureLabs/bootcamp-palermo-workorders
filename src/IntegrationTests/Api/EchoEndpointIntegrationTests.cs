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
    public async Task Should_ReturnOkJson_From_UnversionedPath()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldBe("application/json");
    }

    [Test]
    public async Task Should_ReturnOkJson_From_VersionedPath()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldBe("application/json");
    }

    [Test]
    public async Task Should_EchoBackRequestPath_EndToEnd()
    {
        var response = await _client!.GetAsync("/api/echo?foo=bar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeEchoResponse(response);
        payload.Path.ShouldBe("/api/echo");
        payload.QueryString.ShouldBe("?foo=bar");
    }

    [Test]
    public async Task Should_EchoBackCustomHeader_RoundTrip()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.Add("X-Custom", "value123");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeEchoResponse(response);
        payload.Headers.TryGetValue("X-Custom", out var value).ShouldBeTrue();
        value.ShouldBe("value123");
    }

    [Test]
    public async Task Should_RoundTripQueryParameters_InParsedMap()
    {
        var response = await _client!.GetAsync("/api/echo?name=John&age=30");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeEchoResponse(response);
        payload.Query.ShouldNotBeNull();
        payload.Query!["name"].ShouldBe("John");
        payload.Query["age"].ShouldBe("30");
    }

    [Test]
    public async Task Should_IncludeRemoteIpFromRequest_InEchoResponse()
    {
        var response = await _client!.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("remoteIpAddress", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_AccessWithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnJsonWithAllFields_Present()
    {
        var response = await _client!.GetAsync("/api/echo?key=value");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeEchoResponse(response);
        payload.Method.ShouldNotBeNullOrEmpty();
        payload.Path.ShouldNotBeNullOrEmpty();
        payload.Scheme.ShouldNotBeNullOrEmpty();
        payload.Host.ShouldNotBeNullOrEmpty();
        payload.Protocol.ShouldNotBeNullOrEmpty();
        payload.Headers.ShouldNotBeNull();
        payload.Query.ShouldNotBeNull();
    }

    private static async Task<EchoResponse> DeserializeEchoResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<EchoResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
