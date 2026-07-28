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

    [TestCase("/api/features/flags")]
    [TestCase("/api/v1.0/features/flags")]
    public async Task Should_Return200AndJson_When_GetFeatureFlags(string path)
    {
        var response = await _client!.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("flags", out var flagsElem).ShouldBeTrue();
        flagsElem.GetProperty(FeatureFlagsCatalog.SampleFeatureAKey).GetBoolean().ShouldBeTrue();
        flagsElem.GetProperty(FeatureFlagsCatalog.SampleFeatureBKey).GetBoolean().ShouldBeFalse();
    }

    [TestCase("/api/features/flags")]
    [TestCase("/api/v1.0/features/flags")]
    public async Task Should_ExposeFlags_FromOverriddenConfiguration_When_ConfigurationChanged(string path)
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

        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var flagsElem = doc.RootElement.GetProperty("flags");
        flagsElem.GetProperty(FeatureFlagsCatalog.SampleFeatureAKey).GetBoolean().ShouldBeFalse();
        flagsElem.GetProperty(FeatureFlagsCatalog.SampleFeatureBKey).GetBoolean().ShouldBeTrue();
    }

    [TestCase("/api/features/flags")]
    [TestCase("/api/v1.0/features/flags")]
    public async Task Should_MatchDiagnosticsFeatureFlagValues_When_SameConfiguration(string path)
    {
        var diagnostics = await _client!.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.OK);
        var flags = await _client.GetAsync(path);
        flags.StatusCode.ShouldBe(HttpStatusCode.OK);

        var diagnosticsPayload = await diagnostics.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        await using var stream = await flags.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var flagsRoot = doc.RootElement.GetProperty("flags");

        diagnosticsPayload.ShouldNotBeNull();
        flagsRoot.GetProperty(FeatureFlagsCatalog.SampleFeatureAKey).GetBoolean()
            .ShouldBe(diagnosticsPayload!.FeatureFlags.SampleFeatureA);
        flagsRoot.GetProperty(FeatureFlagsCatalog.SampleFeatureBKey).GetBoolean()
            .ShouldBe(diagnosticsPayload.FeatureFlags.SampleFeatureB);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndFlagsProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauthUnversioned = await client.GetAsync("/api/features/flags");
        unauthUnversioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/features/flags");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var okUnversioned = await withKey.GetAsync("/api/features/flags");
        okUnversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/features/flags");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_NotConflictWithDiagnosticsAndLegacyDiagnostics_When_RoutesRegistered()
    {
        var flags = await _client!.GetAsync("/api/features/flags");
        flags.StatusCode.ShouldBe(HttpStatusCode.OK);

        var diagnostics = await _client.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.OK);

        var legacyResponse = await _client.PostAsync("/_diagnostics/reset-db-connections", null);
        legacyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
