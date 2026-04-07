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
    public async Task Should_Return200AndSameHash_When_PostToolsHash_LegacyAndV1Paths()
    {
        using var legacyContent = new StringContent(
            """{"text":"a"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        using var v1Content = new StringContent(
            """{"text":"a"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var legacy = await _client!.PostAsync("/api/tools/hash", legacyContent);
        var v1 = await _client.PostAsync("/api/v1.0/tools/hash", v1Content);

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        var legacyJson = await legacy.Content.ReadAsStringAsync();
        var v1Json = await v1.Content.ReadAsStringAsync();
        legacyJson.ShouldBe(v1Json);
        legacyJson.ShouldContain("SHA-256");
        legacyJson.ShouldContain("ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb");
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
