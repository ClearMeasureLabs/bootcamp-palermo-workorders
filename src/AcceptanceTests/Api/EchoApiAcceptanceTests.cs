using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class EchoApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task GetEcho_Should_Return200AndReflectRequest()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/echo");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{ServerFixture.ApplicationBaseUrl}/api/echo?accept=1");
        request.Headers.TryAddWithoutValidation("X-Acceptance-Probe", "echo");

        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("method").GetString().ShouldBe("GET");
        doc.RootElement.GetProperty("path").GetString().ShouldBe("/api/echo");
        doc.RootElement.GetProperty("queryString").GetString().ShouldBe("?accept=1");
        doc.RootElement.TryGetProperty("headers", out var headers).ShouldBeTrue();
        headers.TryGetProperty("X-Acceptance-Probe", out var probe).ShouldBeTrue();
        probe.GetString().ShouldBe("echo");
    }
}
