using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GuidGeneratorEndpointIntegrationTests
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
    public async Task Should_Return200AndOneGuid_When_PostUnversionedWithEmptyBody()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeResponse(response);
        payload.Count.ShouldBe(1);
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndOneGuid_When_PostVersionedWithEmptyBody()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeResponse(response);
        payload.Count.ShouldBe(1);
        payload.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndRequestedCount_When_CountProvided()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeResponse(response);
        payload.Count.ShouldBe(5);
        payload.Guids.Count.ShouldBe(5);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_Return400_When_CountOutOfRange()
    {
        var zeroResponse = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 0 });
        zeroResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var aboveMaxResponse = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 101 });
        aboveMaxResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/guid-generator", new { });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await DeserializeResponse(unversioned)).Guids.Count.ShouldBe(1);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await DeserializeResponse(versioned)).Guids.Count.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return401WithoutApiKey_When_NonWhitelistedApiRoute()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tools/other-tool", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_ReturnJsonWithExpectedPropertyNames_When_Success()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 2 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("count", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("guids", out var guidsElement).ShouldBeTrue();
        guidsElement.ValueKind.ShouldBe(JsonValueKind.Array);
        guidsElement.GetArrayLength().ShouldBe(2);
    }

    private static async Task<GuidGeneratorResponse> DeserializeResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GuidGeneratorResponse>(json, ConditionalGetEtag.JsonSerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize guid-generator response.");
    }
}
