using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.Api;
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
    public async Task Should_Return200AndJsonGuids_When_PostUnversioned()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var guids = doc.RootElement.GetProperty("guids");
        guids.GetArrayLength().ShouldBe(1);
        Guid.TryParseExact(guids[0].GetString(), "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJsonGuids_When_PostVersioned()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(1);
    }

    [Test]
    public async Task Should_ReturnMultipleGuids_When_PostWithCount()
    {
        var content = JsonContent.Create(new { count = 3 });
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Guids.Length.ShouldBe(3);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_CountOutOfRange()
    {
        var content = JsonContent.Create(new { count = 101 });
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_PostGuidGenerator()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_EnforceRateLimit_When_PostRepeatedly()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.PostAsync("/api/tools/guid-generator", null);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await client.PostAsync("/api/tools/guid-generator", null);
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
