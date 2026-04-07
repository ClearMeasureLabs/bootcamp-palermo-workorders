using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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

    [Test]
    public async Task Should_Return200AndSha256_When_PostUnversionedPath()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Md5.ShouldBeNull();
        payload.Sha1.ShouldBeNull();
    }

    [Test]
    public async Task Should_Return200AndAllHashes_When_IncludeLegacyHashesTrue()
    {
        var response = await _client!.PostAsJsonAsync(
            "/api/v1.0/tools/hash",
            new { text = "abc", includeLegacyHashes = true });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ToolsHashResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Sha256.ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        payload.Md5.ShouldBe("900150983cd24fb0d6963f7d28e17f72");
        payload.Sha1.ShouldBe("a9993e364706816aba3e25717850c26c9cd0d89d");
    }

    [Test]
    public async Task Should_Return400_When_TextMissing()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/hash", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
