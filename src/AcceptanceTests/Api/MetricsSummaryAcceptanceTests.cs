using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class MetricsSummaryAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task Api_MetricsSummary_Returns200AndRequiredJsonFields()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        root.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        root.TryGetProperty("managedMemoryBytes", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen0Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen1Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen2Collections", out _).ShouldBeTrue();
    }
}
