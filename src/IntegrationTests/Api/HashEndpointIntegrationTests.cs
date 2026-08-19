using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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
    public async Task Should_Return200AndValidJson_When_PostValidTextUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeResponse(response);
        payload.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        payload.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public async Task Should_Return200AndValidJson_When_PostValidTextVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeResponse(response);
        payload.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [TestCase("null")]
    [TestCase("missing")]
    [TestCase("whitespace")]
    public async Task Should_Return400_When_PostEmptyOrWhitespaceText(string scenario)
    {
        HttpResponseMessage response = scenario switch
        {
            "null" => await _client!.PostAsJsonAsync("/api/tools/hash", new { text = (string?)null }),
            "missing" => await _client!.PostAsJsonAsync("/api/tools/hash", new { }),
            "whitespace" => await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "   " }),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_HashAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "hello" });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ContentType_Should_BeJson_When_Response()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    private static async Task<HashTextResponse> DeserializeResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<HashTextResponse>(json, ConditionalGetEtag.JsonSerializerOptions)!;
    }
}
