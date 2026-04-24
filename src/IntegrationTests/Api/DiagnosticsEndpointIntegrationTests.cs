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
public class DiagnosticsEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_GetDiagnosticsUnversioned()
    {
        var response = await _client!.GetAsync("/api/diagnostics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetDiagnosticsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/diagnostics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeEnvironment_FromHost_When_GetDiagnostics()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Environment.ShouldBe("Testing");
    }

    [Test]
    public async Task Should_ExposeNonNegativeUptime_When_GetDiagnostics()
    {
        var before = DateTime.UtcNow;
        var response = await _client!.GetAsync("/api/diagnostics");
        var after = DateTime.UtcNow;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        var health = SimpleHealthResponseBuilder.Build(TimeProvider.System);
        (after - before).ShouldBeLessThan(TimeSpan.FromSeconds(30));
        payload.Uptime.ShouldBeLessThanOrEqualTo(health.Uptime + TimeSpan.FromSeconds(2));
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(health.Uptime - TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Should_ExposeFeatureFlags_FromConfiguration_When_GetDiagnostics()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.FeatureFlags.SampleFeatureA.ShouldBeTrue();
        payload.FeatureFlags.SampleFeatureB.ShouldBeFalse();
    }

    [Test]
    public async Task Should_ExposeFeatureFlags_FromOverriddenConfiguration_When_GetDiagnostics()
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

        var response = await client.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.FeatureFlags.SampleFeatureA.ShouldBeFalse();
        payload.FeatureFlags.SampleFeatureB.ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeSameFeatureFlags_AsDiagnostics_When_GetFeatureFlags()
    {
        var diag = await _client!.GetAsync("/api/diagnostics");
        var flags = await _client.GetAsync("/api/features/flags");
        diag.StatusCode.ShouldBe(HttpStatusCode.OK);
        flags.StatusCode.ShouldBe(HttpStatusCode.OK);

        var diagPayload = await diag.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var flagsPayload = await flags.Content.ReadFromJsonAsync<RuntimeFeatureFlagsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        diagPayload.ShouldNotBeNull();
        flagsPayload.ShouldNotBeNull();
        flagsPayload!.FeatureFlags.SampleFeatureA.ShouldBe(diagPayload!.FeatureFlags.SampleFeatureA);
        flagsPayload.FeatureFlags.SampleFeatureB.ShouldBe(diagPayload.FeatureFlags.SampleFeatureB);
    }

    [Test]
    public async Task Should_NotConflictWithLegacyDiagnostics_When_RoutesRegistered()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var legacyResponse = await _client.PostAsync("/_diagnostics/reset-db-connections", null);
        legacyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndDiagnosticsProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/diagnostics");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/diagnostics");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var flagsUnauth = await client.GetAsync("/api/features/flags");
        flagsUnauth.StatusCode.ShouldBe(HttpStatusCode.OK);
        var flagsVersionedUnauth = await client.GetAsync("/api/v1.0/features/flags");
        flagsVersionedUnauth.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/diagnostics");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/diagnostics");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
