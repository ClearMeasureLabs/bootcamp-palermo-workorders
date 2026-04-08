using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiVersioningEndpointTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiVersioningRoutingWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndSamePayload_When_GetSimpleHealth_LegacyAndV1Paths()
    {
        var legacy = await _client!.GetAsync("/api/health");
        var v1 = await _client.GetAsync("/api/v1.0/health");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var legacyDoc = JsonDocument.Parse(await legacy.Content.ReadAsStringAsync());
        using var v1Doc = JsonDocument.Parse(await v1.Content.ReadAsStringAsync());
        legacyDoc.RootElement.GetProperty("status").GetString().ShouldBe(v1Doc.RootElement.GetProperty("status").GetString());
        legacyDoc.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    [Test]
    public async Task Should_Return200AndEquivalentComponentReport_When_GetDetailedHealth_LegacyAndV1Paths()
    {
        var legacy = await _client!.GetAsync("/api/health/detailed");
        var v1 = await _client.GetAsync("/api/v1.0/health/detailed");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var legacyDoc = JsonDocument.Parse(await legacy.Content.ReadAsStringAsync());
        using var v1Doc = JsonDocument.Parse(await v1.Content.ReadAsStringAsync());

        legacyDoc.RootElement.GetProperty("overallStatus").GetString()
            .ShouldBe(v1Doc.RootElement.GetProperty("overallStatus").GetString());

        var legacyComponents = ReadComponentStatuses(legacyDoc.RootElement.GetProperty("components"));
        var v1Components = ReadComponentStatuses(v1Doc.RootElement.GetProperty("components"));
        legacyComponents.Count.ShouldBe(v1Components.Count);
        legacyComponents.ShouldBeEquivalentTo(v1Components);
    }

    private static Dictionary<string, string> ReadComponentStatuses(JsonElement componentsArray)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in componentsArray.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString() ?? "";
            var status = item.GetProperty("status").GetString() ?? "";
            map[name] = status;
        }

        return map;
    }

    [Test]
    public async Task Should_ReturnNotSuccess_When_GetSimpleHealth_UnsupportedVersion()
    {
        var response = await _client!.GetAsync("/api/v2.0/health");

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200_When_GetVersion_V1Path()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_Return200_When_GetTime_V1Path()
    {
        var response = await _client!.GetAsync("/api/v1.0/time");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
    }

    [Test]
    public async Task Should_IncludeSupportedVersionsHeader_When_GetVersionedEndpoint()
    {
        var response = await _client!.GetAsync("/api/v1.0/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("api-supported-versions", out var values).ShouldBeTrue();
        values.ShouldNotBeNull();
        string.Join(", ", values!).ShouldContain("1.0");
    }
}
