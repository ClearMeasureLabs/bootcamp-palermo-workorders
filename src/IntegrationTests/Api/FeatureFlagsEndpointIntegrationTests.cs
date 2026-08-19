using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
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
    public async Task Should_Return200AndJson_When_GetFeaturesUnversioned()
    {
        var response = await _client!.GetAsync("/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetFeaturesVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Test]
    public async Task Should_ExposeFlagsFromStaticDictionary_When_GetFeatures()
    {
        var response = await _client!.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            json,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        var expected = ApplicationFeatureFlags.GetAll();
        payload!.Count.ShouldBe(expected.Count);
        foreach (var (key, value) in expected)
            payload[key].ShouldBe(value);
        payload["sampleFeatureA"].ShouldBeTrue();
        payload["sampleFeatureB"].ShouldBeFalse();
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndFeaturesProtected()
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
}
