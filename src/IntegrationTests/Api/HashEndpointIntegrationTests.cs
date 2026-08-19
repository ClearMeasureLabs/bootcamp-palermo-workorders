using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HashEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_PostUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<HashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Return200AndJson_When_PostVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_ReturnValidSha256_When_PostWithText()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "integration-test" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Sha256.Length.ShouldBe(64);
        payload.Sha256.ShouldMatch("^[0-9a-f]+$");
    }

    [Test]
    public async Task Should_Return400_When_PostWithoutText()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("detail");
    }

    [Test]
    public async Task Should_Return400_When_TextIsNull()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_PassRateLimiting_When_PollingNormal()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "rate-limit-ok" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_HitRateLimiter_When_ExceedingThreshold()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/tools/hash", new { text = "first" })).StatusCode
            .ShouldBe(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/tools/hash", new { text = "second" })).StatusCode
            .ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task Should_WorkWithEmptyText()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Test]
    public async Task Should_WorkWithLargeText()
    {
        var largeText = new string('x', 1024 * 1024);
        using var content = new StringContent(
            JsonSerializer.Serialize(new { text = largeText }),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/tools/hash", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Sha256.Length.ShouldBe(64);
        payload.Sha256.ShouldMatch("^[0-9a-f]+$");
    }
}
