using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class FeatureFlagsEndpointIntegrationTests
{
    private FeatureFlagsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new FeatureFlagsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetFeatureFlagsUnversioned()
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
            (property.Value.ValueKind == JsonValueKind.True
                || property.Value.ValueKind == JsonValueKind.False).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetFeatureFlagsVersioned()
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
            (property.Value.ValueKind == JsonValueKind.True
                || property.Value.ValueKind == JsonValueKind.False).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_ReturnStaticFlagValues_NotConfigBoundDiagnosticsFlags_When_GetFeatureFlags()
    {
        await using var factory = new FeatureFlagsWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:SampleFeatureA"] = "false",
                    ["FeatureFlags:SampleFeatureB"] = "true"
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await DeserializeFlagsAsync(response);
        payload.Count.ShouldBe(ApplicationFeatureFlags.All.Count);
        foreach (var (key, expected) in ApplicationFeatureFlags.All)
        {
            payload.TryGetValue(key, out var actual).ShouldBeTrue($"missing flag '{key}'");
            actual.ShouldBe(expected);
        }
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndEndpointProtected()
    {
        await using var factory = new FeatureFlagsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/features/flags");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/features/flags");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/features/flags");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/features/flags");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<Dictionary<string, bool>> DeserializeFlagsAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, bool>>(
            stream,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
