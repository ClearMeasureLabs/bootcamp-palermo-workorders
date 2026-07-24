using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HelloEndpointIntegrationTests
{
    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJsonMessage_When_GetHello()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json; charset=utf-8");
        var payload = JsonSerializer.Deserialize<HelloResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_Return200AndJsonMessage_When_GetHelloVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json; charset=utf-8");
        var payload = JsonSerializer.Deserialize<HelloResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_NoAuthHeaders()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/hello");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedPayload = JsonSerializer.Deserialize<HelloResponse>(
            await unversioned.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        unversionedPayload!.Message.ShouldBe("Hello, World!");

        var versioned = await client.GetAsync("/api/v1.0/hello");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedPayload = JsonSerializer.Deserialize<HelloResponse>(
            await versioned.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        versionedPayload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_RespectRateLimiting_When_ExcessiveRequests()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "2",
            ["ApiRateLimiting:WindowSeconds"] = "2",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/hello")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/hello")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/hello")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
