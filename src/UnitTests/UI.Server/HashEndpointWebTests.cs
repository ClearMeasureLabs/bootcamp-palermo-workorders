using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HashEndpointWebTests
{
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

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
    public async Task Should_Return200AndSameSha256_When_PostUnversionedAndVersioned()
    {
        using var unversionedContent = JsonContent.Create(new { text = "abc" });
        using var versionedContent = JsonContent.Create(new { text = "abc" });

        var unversioned = await _client!.PostAsync("/api/tools/hash", unversionedContent);
        var versioned = await _client.PostAsync("/api/v1.0/tools/hash", versionedContent);

        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        unversioned.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        versioned.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        using var unversionedDoc = JsonDocument.Parse(await unversioned.Content.ReadAsStringAsync());
        using var versionedDoc = JsonDocument.Parse(await versioned.Content.ReadAsStringAsync());
        var unversionedSha = unversionedDoc.RootElement.GetProperty("sha256").GetString();
        var versionedSha = versionedDoc.RootElement.GetProperty("sha256").GetString();
        unversionedSha.ShouldBe(AbcSha256);
        versionedSha.ShouldBe(unversionedSha);
    }

    [Test]
    public async Task Should_AllowAnonymousPost_WithoutApiKey_When_ToolsHash()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("""{"text":"abc"}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/tools/hash", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sha256").GetString().ShouldBe(AbcSha256);
    }
}
