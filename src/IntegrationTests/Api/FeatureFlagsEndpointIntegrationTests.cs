using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
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
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetFeatureFlagsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_ExposeFlatCamelCaseFlagProperties_When_GetFeatureFlags()
    {
        var response = await _client!.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("sampleFeatureA", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("sampleFeatureB", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeFalse();
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

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!["sampleFeatureA"].ShouldBeFalse();
        payload["sampleFeatureB"].ShouldBeTrue();
    }

    [Test]
    public async Task Should_AlignWithDiagnosticsFlagValues_When_BothEndpointsCalled()
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

        var flagsResponse = await client.GetAsync("/api/features/flags");
        flagsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var flags = await flagsResponse.Content.ReadFromJsonAsync<Dictionary<string, bool>>(
            ConditionalGetEtag.JsonSerializerOptions);

        var diagnosticsResponse = await client.GetAsync("/api/diagnostics");
        diagnosticsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var diagnostics = await diagnosticsResponse.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        flags.ShouldNotBeNull();
        diagnostics.ShouldNotBeNull();
        flags!["sampleFeatureA"].ShouldBe(diagnostics!.FeatureFlags.SampleFeatureA);
        flags["sampleFeatureB"].ShouldBe(diagnostics.FeatureFlags.SampleFeatureB);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndFeatureFlagsProtected()
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

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var first = await _client!.GetAsync("/api/features/flags");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag?.Tag;
        etag.ShouldNotBeNullOrWhiteSpace();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/features/flags");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var second = await _client.SendAsync(request);

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await second.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task Should_NotAlterDiagnosticsEndpoint_When_FeatureFlagsRouteAdded()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
    }
}
