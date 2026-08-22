using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class FeatureFlagsEndpointIntegrationTests
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
    public async Task Should_Return200AndJsonObject_When_GetFeatureFlagsUnversioned()
    {
        var response = await _client!.GetAsync("/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            property.Value.ValueKind.ShouldBeOneOf(JsonValueKind.True, JsonValueKind.False);
        }
    }

    [Test]
    public async Task Should_Return200AndJsonObject_When_GetFeatureFlagsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            property.Value.ValueKind.ShouldBeOneOf(JsonValueKind.True, JsonValueKind.False);
        }
    }

    [Test]
    public async Task Should_ExposeStaticCatalogFlags_When_GetFeatureFlags()
    {
        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:SampleFeatureA"] = "false",
                    ["FeatureFlags:SampleFeatureB"] = "true",
                    ["FeatureFlags:EnableAdvancedSearch"] = "false",
                    ["FeatureFlags:EnableLegacyReports"] = "true"
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var expected = FeatureFlagsCatalog.GetAll();
        foreach (var pair in expected)
        {
            doc.RootElement.TryGetProperty(pair.Key, out var value).ShouldBeTrue();
            value.GetBoolean().ShouldBe(pair.Value);
        }

        doc.RootElement.TryGetProperty("sampleFeatureA", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("SampleFeatureA", out _).ShouldBeFalse();
    }

    [Test]
    public async Task Should_AllowAnonymous_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/features/flags");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/features/flags");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
