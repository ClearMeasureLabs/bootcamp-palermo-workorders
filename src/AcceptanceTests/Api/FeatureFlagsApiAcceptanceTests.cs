using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class FeatureFlagsApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task GetFeatureFlags_Should_Return200WithJsonDictionary_When_EndpointCalled()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/features/flags");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(doc.RootElement.GetRawText());
        dict.ShouldNotBeNull();
        dict.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GetFeatureFlags_Should_Return200WithJsonDictionary_When_VersionedEndpointCalled()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/v1.0/features/flags");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(doc.RootElement.GetRawText());
        dict.ShouldNotBeNull();
        dict.Count.ShouldBeGreaterThan(0);
    }
}
