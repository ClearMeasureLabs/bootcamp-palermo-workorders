using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsHashIntegrationTests
{
    private const string ExpectedSha256Abc =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private ToolsHashWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ToolsHashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200_WithCorrectSha256_ForKnownInput_OnUnversionedRoute()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new ToolsHashRequest { Text = "abc" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertSha256Async(response, ExpectedSha256Abc);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotContain("\"md5\"");
        json.ShouldNotContain("\"sha1\"");
    }

    [Test]
    public async Task Should_Return200_WithCorrectSha256_OnVersionedRoute()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/hash", new ToolsHashRequest { Text = "abc" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertSha256Async(response, ExpectedSha256Abc);
    }

    [Test]
    public async Task Should_OmitLegacyHashes_ByDefault_When_FlagFalseOrAbsent()
    {
        var response = await _client!.PostAsJsonAsync(
            "/api/tools/hash",
            new ToolsHashRequest { Text = "abc", IncludeLegacyHashes = false });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("md5", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("sha1", out _).ShouldBeFalse();
    }

    [Test]
    public async Task Should_IncludeMd5AndSha1_When_LegacyFlagTrue()
    {
        var response = await _client!.PostAsJsonAsync(
            "/api/tools/hash",
            new ToolsHashRequest { Text = "abc", IncludeLegacyHashes = true });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe(ExpectedSha256Abc);
        payload.Md5.ShouldBe("900150983cd24fb0d6963f7d28e17f72");
        payload.Sha1.ShouldBe("a9993e364706816aba3e25717850c26c9cd0d89d");
    }

    [Test]
    public async Task Should_Return400_When_TextMissing()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var hasValidationErrors = root.TryGetProperty("errors", out _);
        var hasProblemShape = root.TryGetProperty("title", out _) && root.TryGetProperty("status", out _);
        (hasValidationErrors || hasProblemShape).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return415_When_NonJsonContentType()
    {
        using var content = new StringContent("hello", Encoding.UTF8, "text/plain");
        var response = await _client!.PostAsync("/api/tools/hash", content);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Should_RequireApiKey_When_Enabled_And_PathNotPublic()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.PostAsJsonAsync("/api/tools/hash", new ToolsHashRequest { Text = "x" });
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.PostAsJsonAsync("/api/tools/hash", new ToolsHashRequest { Text = "abc" });
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertSha256Async(ok, ExpectedSha256Abc);
    }

    private static async Task AssertSha256Async(HttpResponseMessage response, string expectedHex)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("sha256").GetString().ShouldBe(expectedHex);
    }
}
