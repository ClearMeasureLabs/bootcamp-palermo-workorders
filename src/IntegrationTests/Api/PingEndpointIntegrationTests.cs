using System.Globalization;
using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class PingEndpointIntegrationTests
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
        var response = await _client!.GetAsync("/api/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldBe("application/json");
        await AssertValidPingJson(await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldBe("application/json");
        await AssertValidPingJson(await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Should_ValidateJsonSchema_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        AssertValidIso8601Timestamp(body);
    }

    [Test]
    public async Task Should_ValidateJsonSchema_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        AssertValidIso8601Timestamp(body);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PingAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/ping");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertValidPingJson(await unversioned.Content.ReadAsStringAsync());

        var versioned = await client.GetAsync("/api/v1.0/ping");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertValidPingJson(await versioned.Content.ReadAsStringAsync());
    }

    private static Task AssertValidPingJson(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("pong").GetString().ShouldBe("pong");
        AssertValidIso8601Timestamp(body);
        return Task.CompletedTask;
    }

    private static void AssertValidIso8601Timestamp(string body)
    {
        using var document = JsonDocument.Parse(body);
        var timestamp = document.RootElement.GetProperty("timestamp").GetString();
        timestamp.ShouldNotBeNullOrEmpty();
        DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _).ShouldBeTrue();
    }
}
