using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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
    public async Task Should_Return200JsonGuid_When_PostUnversioned()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        payload.ShouldNotBeNull();
        payload!.Count.ShouldBe(1);
        payload.Guids.Length.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200JsonGuid_When_PostVersioned()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        payload.ShouldNotBeNull();
        payload!.Count.ShouldBe(1);
        payload.Guids.Length.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return200_When_PostWithCountParameter()
    {
        using var content = new StringContent("{\"count\":3}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        payload.ShouldNotBeNull();
        payload!.Count.ShouldBe(3);
        payload.Guids.Length.ShouldBe(3);
        payload.Guids.Distinct().Count().ShouldBe(3);
    }

    [Test]
    public async Task Should_Return400_When_CountExceedsMax()
    {
        using var content = new StringContent("{\"count\":101}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().ShouldBe(400);
    }

    [Test]
    public async Task Should_Return400_When_CountIsZero()
    {
        using var content = new StringContent("{\"count\":0}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().ShouldBe(400);
    }

    [Test]
    public async Task Should_ReturnAnonymousOk_WhenNoAuth()
    {
        using var anonymousClient = _factory!.CreateClient();
        var response = await anonymousClient.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_RespectRateLimiter_WhenPolicySlidingWindow()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "2",
            ["ApiRateLimiting:WindowSeconds"] = "60",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };

        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/tools/guid-generator", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync("/api/tools/guid-generator", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync("/api/tools/guid-generator", null)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
