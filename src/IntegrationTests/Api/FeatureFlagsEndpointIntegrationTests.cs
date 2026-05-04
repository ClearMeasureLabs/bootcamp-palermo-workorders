using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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
        doc.RootElement.TryGetProperty("flags", out var flags).ShouldBeTrue();
        flags.ValueKind.ShouldBe(JsonValueKind.Array);
        flags.GetArrayLength().ShouldBe(2);
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
        doc.RootElement.TryGetProperty("flags", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeFlagValues_FromConfiguration_When_GetFeatureFlags()
    {
        var response = await _client!.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<FeatureFlagsResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Flags.ShouldContain(f => f.Name == "SampleFeatureA" && f.Enabled);
        payload.Flags.ShouldContain(f => f.Name == "SampleFeatureB" && !f.Enabled);
    }

    [Test]
    public async Task Should_ExposeFlagValues_FromOverriddenConfiguration_When_GetFeatureFlags()
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

        var payload = await response.Content.ReadFromJsonAsync<FeatureFlagsResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Flags.ShouldContain(f => f.Name == "SampleFeatureA" && !f.Enabled);
        payload.Flags.ShouldContain(f => f.Name == "SampleFeatureB" && f.Enabled);
    }

    [Test]
    public async Task Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var first = await _client!.GetAsync("/api/features/flags");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/features/flags");
        second.Headers.IfNoneMatch.Add(etag);

        var cached = await _client!.SendAsync(second);
        cached.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }
}
