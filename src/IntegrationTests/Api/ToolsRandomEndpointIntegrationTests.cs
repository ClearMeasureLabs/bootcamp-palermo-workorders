using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
    public async Task Should_Return200AndJson_When_GetUnversionedUuid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeAsync(response);
        payload.Type.ShouldBe("uuid");
        Guid.TryParse(payload.Value, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersionedUuid()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeAsync(response);
        payload.Type.ShouldBe("uuid");
        Guid.TryParse(payload.Value, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_ToolsRandomAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/random?type=number");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await DeserializeAsync(unversioned)).Type.ShouldBe("number");

        var versioned = await client.GetAsync("/api/v1.0/tools/random?type=color");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await DeserializeAsync(versioned)).Type.ShouldBe("color");
    }

    [Test]
    public async Task Should_Return400_When_TypeInvalid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=nope");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task<RandomPayload> DeserializeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<RandomPayload>(json, JsonOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }

    private sealed record RandomPayload(string Type, string Value);
}
