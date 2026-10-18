using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class VersionEndpointAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task Should_Return200WithNonEmptyVersion_When_GetVersionEndpoint()
    {
        var response = await Page.APIRequest.GetAsync("/_version");

        response.Status.ShouldBe(200);
        var body = await response.TextAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("version", out var versionProp).ShouldBeTrue();
        versionProp.GetString().ShouldNotBeNullOrEmpty();
    }
}
