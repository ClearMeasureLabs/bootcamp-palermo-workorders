using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsHashEndpointIntegrationTests
{
    private const string AbcSha256 =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string AbcMd5 = "900150983cd24fb0d6963f7d28e17f72";
    private const string AbcSha1 = "a9993e364706816aba3e25717850c26c9cd0d89d";

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

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
    public async Task Should_Return200WithSha256_When_PostUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "abc" });

        await AssertSha256OnlyAsync(response, AbcSha256);
    }

    [Test]
    public async Task Should_Return200WithSha256_When_PostVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "abc" });

        await AssertSha256OnlyAsync(response, AbcSha256);
    }

    [TestCase("/api/tools/hash")]
    [TestCase("/api/v1.0/tools/hash")]
    public async Task Should_Return400_When_TextMissing(string path)
    {
        var missingField = await _client!.PostAsJsonAsync(path, new { });
        missingField.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var nullText = await _client!.PostAsJsonAsync(path, new { text = (string?)null });
        nullText.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ReturnOptionalHashes_When_FlagsRequested()
    {
        var withoutFlags = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "abc" });
        await AssertSha256OnlyAsync(withoutFlags, AbcSha256);

        var withFlags = await _client!.PostAsJsonAsync(
            "/api/tools/hash",
            new { text = "abc", includeMd5 = true, includeSha1 = true });
        withFlags.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await withFlags.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("sha256").GetString().ShouldBe(AbcSha256);
        payload.GetProperty("md5").GetString().ShouldBe(AbcMd5);
        payload.GetProperty("sha1").GetString().ShouldBe(AbcSha1);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/hash", new { text = "abc" });
        await AssertSha256OnlyAsync(unversioned, AbcSha256);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "abc" });
        await AssertSha256OnlyAsync(versioned, AbcSha256);
    }

    private static async Task AssertSha256OnlyAsync(HttpResponseMessage response, string expectedSha256)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("sha256").GetString().ShouldBe(expectedSha256);
        root.TryGetProperty("md5", out _).ShouldBeFalse();
        root.TryGetProperty("sha1", out _).ShouldBeFalse();
    }
}
