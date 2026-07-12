using System.Net;
using System.Net.Http.Json;
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
    public async Task Should_Return200AndJson_When_GetFeatureFlagsUnversioned()
    {
        var response = await _client!.GetAsync("/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("sampleFeatureA", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("sampleFeatureB", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeFalse();
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
        doc.RootElement.TryGetProperty("sampleFeatureA", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("sampleFeatureB", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeFeatureFlags_FromConfiguration_When_GetFeatureFlags()
    {
        var response = await _client!.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!["sampleFeatureA"].ShouldBeTrue();
        payload["sampleFeatureB"].ShouldBeFalse();
    }

    [Test]
    public async Task Should_ExposeFeatureFlags_FromOverriddenConfiguration_When_GetFeatureFlags()
    {
        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
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

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!["sampleFeatureA"].ShouldBeFalse();
        payload["sampleFeatureB"].ShouldBeTrue();
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndEndpointProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
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

    [Test]
    public async Task Should_NotAlterDiagnostics_When_FeatureFlagsEndpointRegistered()
    {
        var diagnostics = await _client!.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var diagStream = await diagnostics.Content.ReadAsStreamAsync();
        using var diagDoc = await JsonDocument.ParseAsync(diagStream);
        diagDoc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();

        var flags = await _client.GetAsync("/api/features/flags");
        flags.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var flagsStream = await flags.Content.ReadAsStreamAsync();
        using var flagsDoc = await JsonDocument.ParseAsync(flagsStream);
        flagsDoc.RootElement.TryGetProperty("sampleFeatureA", out _).ShouldBeTrue();
        flagsDoc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeFalse();
    }

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var first = await _client!.GetAsync("/api/features/flags");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/features/flags");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }
}
