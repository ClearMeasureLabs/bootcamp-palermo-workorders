using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsHashEndpointIntegrationTests
{
    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    private static ToolsHashResponse ExpectedForText(string text)
    {
        var utf8 = Encoding.UTF8.GetBytes(text);
        return new ToolsHashResponse
        {
            Sha256 = Convert.ToHexString(SHA256.HashData(utf8)).ToLowerInvariant(),
            Md5 = Convert.ToHexString(MD5.HashData(utf8)).ToLowerInvariant(),
            Sha1 = Convert.ToHexString(SHA1.HashData(utf8)).ToLowerInvariant()
        };
    }

    [Test]
    public async Task Should_Return200AndCorrectDigests_When_PostValidText_UnversionedRoute()
    {
        const string text = "abc";
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        var expected = ExpectedForText(text);
        payload!.Sha256.ShouldBe(expected.Sha256);
        payload.Md5.ShouldBe(expected.Md5);
        payload.Sha1.ShouldBe(expected.Sha1);
    }

    [Test]
    public async Task Should_Return200AndSameDigests_When_PostValidText_VersionedRoute()
    {
        const string text = "abc";
        var unversioned = await _client!.PostAsJsonAsync("/api/tools/hash", new { text });
        var versioned = await _client!.PostAsJsonAsync("/api/v1.0/tools/hash", new { text });

        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var a = await unversioned.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var b = await versioned.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        a.ShouldNotBeNull();
        b.ShouldNotBeNull();
        b!.Sha256.ShouldBe(a!.Sha256);
        b.Md5.ShouldBe(a.Md5);
        b.Sha1.ShouldBe(a.Sha1);
    }

    [Test]
    public async Task Should_Return200AndKnownVectors_When_TextIsEmptyString()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var expected = ExpectedForText("");
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe(expected.Sha256);
        payload.Md5.ShouldBe(expected.Md5);
        payload.Sha1.ShouldBe(expected.Sha1);
    }

    [Test]
    public async Task Should_Return400_When_TextMissing()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_TextNull()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = (string?)null });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400Or415_When_BodyIsNotValidJson()
    {
        using var content = new StringContent("{not json", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/hash", content);
        (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType)
            .ShouldBeTrue();
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.PostAsJsonAsync("/api/tools/hash", new { text = "x" });
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "x" });
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.PostAsJsonAsync("/api/tools/hash", new { text = "x" });
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "x" });
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnDeterministicCachedResponse_When_IdempotencyKeyReplayed()
    {
        const string bodyJson = """{"text":"idem"}""";
        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/tools/hash");
        req1.Headers.Add(IdempotencyConstants.HeaderName, "tools-hash-idem");
        req1.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var r1 = await _client!.SendAsync(req1);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body1 = await r1.Content.ReadAsStringAsync();

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/tools/hash");
        req2.Headers.Add(IdempotencyConstants.HeaderName, "tools-hash-idem");
        req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var r2 = await _client.SendAsync(req2);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body2 = await r2.Content.ReadAsStringAsync();

        body2.ShouldBe(body1);
    }

    [Test]
    public async Task Should_HashUtf8Bytes_When_InputContainsNonAscii()
    {
        const string text = "café";
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var expected = ExpectedForText(text);
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe(expected.Sha256);
        payload.Md5.ShouldBe(expected.Md5);
        payload.Sha1.ShouldBe(expected.Sha1);
    }
}
