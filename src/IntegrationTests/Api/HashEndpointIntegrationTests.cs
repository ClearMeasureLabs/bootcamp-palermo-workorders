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
    public async Task Should_Return200AndJsonWithHashes_When_PostUnversioned()
    {
        var response = await PostHashAsync(_client!, "/api/tools/hash", """{"text":"hello"}""");

        await AssertHashResponseAsync(response);
    }

    [Test]
    public async Task Should_Return200AndJsonWithHashes_When_PostVersioned()
    {
        var response = await PostHashAsync(_client!, "/api/v1.0/tools/hash", """{"text":"hello"}""");

        await AssertHashResponseAsync(response);
    }

    [Test]
    public async Task Should_Return400ProblemJson_When_TextMissingOrEmpty()
    {
        foreach (var body in new[] { "{}", """{"text":""}""", """{"text":"   "}""" })
        {
            var response = await PostHashAsync(_client!, "/api/tools/hash", body);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            mediaType.ShouldNotBeNull();
            mediaType!.ShouldContain("application/problem+json");

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("status", out _).ShouldBeTrue();
            doc.RootElement.TryGetProperty("title", out _).ShouldBeTrue();
            doc.RootElement.TryGetProperty("type", out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndHashProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await PostHashAsync(client, "/api/tools/hash", """{"text":"hello"}""");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await PostHashAsync(client, "/api/v1.0/tools/hash", """{"text":"hello"}""");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await PostHashAsync(withKey, "/api/tools/hash", """{"text":"hello"}""");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await PostHashAsync(withKey, "/api/v1.0/tools/hash", """{"text":"hello"}""");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnConsistentHashes_When_SameInputPostedTwice()
    {
        var first = await PostHashAsync(_client!, "/api/tools/hash", """{"text":"hello"}""");
        var second = await PostHashAsync(_client!, "/api/tools/hash", """{"text":"hello"}""");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);

        var firstPayload = await first.Content.ReadFromJsonAsync<HashTextResponse>();
        var secondPayload = await second.Content.ReadFromJsonAsync<HashTextResponse>();
        firstPayload.ShouldNotBeNull();
        secondPayload.ShouldNotBeNull();
        firstPayload!.Sha256.ShouldBe(secondPayload!.Sha256);
        firstPayload.Md5.ShouldBe(secondPayload.Md5);
        firstPayload.Sha1.ShouldBe(secondPayload.Sha1);
    }

    private static async Task<HttpResponseMessage> PostHashAsync(HttpClient client, string path, string jsonBody)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return await client.PostAsync(path, content);
    }

    private static async Task AssertHashResponseAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<HashTextResponse>();
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        payload.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }
}
