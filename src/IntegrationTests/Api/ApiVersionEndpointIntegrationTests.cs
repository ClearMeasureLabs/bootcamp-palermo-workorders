using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ApiVersionEndpointIntegrationTests
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
    public async Task Should_Return200JsonMetadata_When_GetUnversionedApiVersion()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionMetadataPayloadAsync(response);
    }

    [Test]
    public async Task Should_Return200JsonMetadata_When_GetVersionedApiVersion()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionMetadataPayloadAsync(response);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionMetadataPayloadAsync(unversioned);

        var versioned = await client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionMetadataPayloadAsync(versioned);
    }

    private static async Task AssertVersionMetadataPayloadAsync(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

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
        root.TryGetProperty(name, out var property).ShouldBeTrue($"missing JSON property '{name}'");
        property.ValueKind.ShouldBe(JsonValueKind.String);
        property.GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
