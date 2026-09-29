using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class VersionApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task GetVersion_Should_Return200WithMetadataFields_When_EndpointCalled()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/version");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        await AssertVersionMetadataPayloadAsync(response);
    }

    [Test]
    public async Task GetVersion_Should_Return200WithMetadataFields_When_VersionedEndpointCalled()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/v1.0/version");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        await AssertVersionMetadataPayloadAsync(response);
    }

    private static async Task AssertVersionMetadataPayloadAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);

        AssertNonEmptyStringProperty(doc.RootElement, "assemblyVersion");
        AssertNonEmptyStringProperty(doc.RootElement, "informationalVersion");
        AssertNonEmptyStringProperty(doc.RootElement, "buildConfiguration");
        AssertNonEmptyStringProperty(doc.RootElement, "environment");
    }

    private static void AssertNonEmptyStringProperty(JsonElement root, string name)
    {
        root.TryGetProperty(name, out var property).ShouldBeTrue();
        property.ValueKind.ShouldBe(JsonValueKind.String);
        property.GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
