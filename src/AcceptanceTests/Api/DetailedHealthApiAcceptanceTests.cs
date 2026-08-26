using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

/// <summary>
/// Full-system coverage for <c>GET /api/health/detailed</c> against the running UI.Server.
/// </summary>
[TestFixture]
public class DetailedHealthApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task GetDetailedHealth_Returns200JsonWithOverallStatusAndComponents()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.TryGetProperty("overallStatus", out var overallStatus).ShouldBeTrue();
        overallStatus.GetString().ShouldNotBeNullOrWhiteSpace();

        root.TryGetProperty("components", out var components).ShouldBeTrue();
        components.ValueKind.ShouldBe(JsonValueKind.Array);
        components.GetArrayLength().ShouldBeGreaterThan(0);

        var first = components[0];
        first.TryGetProperty("name", out var name).ShouldBeTrue();
        name.GetString().ShouldNotBeNullOrWhiteSpace();
        first.TryGetProperty("status", out var status).ShouldBeTrue();
        status.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task GetDetailedHealth_VersionedRoute_Returns200Json()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/v1.0/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.TryGetProperty("overallStatus", out var overallStatus).ShouldBeTrue();
        overallStatus.GetString().ShouldNotBeNullOrWhiteSpace();

        root.TryGetProperty("components", out var components).ShouldBeTrue();
        components.ValueKind.ShouldBe(JsonValueKind.Array);
        components.GetArrayLength().ShouldBeGreaterThan(0);
    }
}
