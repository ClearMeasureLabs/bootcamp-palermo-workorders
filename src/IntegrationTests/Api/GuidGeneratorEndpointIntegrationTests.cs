using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.Api;
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
    public async Task Should_Return200WithGuids_When_PostUnversionedEndpoint()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithGuids_When_PostVersionedEndpoint()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return200WithCountGuids_When_PostWithCountParameter()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 10 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(10);
        foreach (var guid in payload.Guids)
        {
            Guid.TryParse(guid, out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_Return400_When_PostWithInvalidCount()
    {
        var tooMany = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 101 });
        tooMany.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var zero = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 0 });
        zero.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var negative = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = -1 });
        negative.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Respect_RateLimiting_When_PostMultipleTimes()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/tools/guid-generator", new { })).StatusCode
            .ShouldBe(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/tools/guid-generator", new { })).StatusCode
            .ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
