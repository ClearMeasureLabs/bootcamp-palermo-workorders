using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class VersionApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task GetVersion_Should_Return200_WithExpectedJsonFields()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/version");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.GetProperty("assemblyVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("informationalVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("buildConfiguration").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("environment").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("machineName").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrEmpty();
    }
}
