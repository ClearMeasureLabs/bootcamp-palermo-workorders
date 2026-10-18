using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class VersionEndpointAcceptanceTests
{
    [Test]
    public async Task Should_Return200WithNonEmptyVersion_When_GetVersionEndpoint()
    {
        using var client = new HttpClient { BaseAddress = new Uri(ServerFixture.BaseUrl) };

        var response = await client.GetAsync("/_version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("version", out var versionProp).ShouldBeTrue();
        versionProp.GetString().ShouldNotBeNullOrEmpty();
    }
}
