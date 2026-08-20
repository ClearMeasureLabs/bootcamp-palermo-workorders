using System.Net;
using System.Net.Http.Json;
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
    public async Task Should_Return200WithHashFields_When_PostUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadFromJsonAsync<HashResponse>();
        body.ShouldNotBeNull();
        body!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        body.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        body.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public async Task Should_Return200WithHashFields_When_PostVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadFromJsonAsync<HashResponse>();
        body.ShouldNotBeNull();
        body!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        body.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        body.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public async Task Should_Return400_When_TextMissing()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_HashAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedBody = await unversioned.Content.ReadFromJsonAsync<HashResponse>();
        unversionedBody!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "hello" });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedBody = await versioned.Content.ReadFromJsonAsync<HashResponse>();
        versionedBody!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    private sealed record HashResponse(string Sha256, string Md5, string Sha1);
}
