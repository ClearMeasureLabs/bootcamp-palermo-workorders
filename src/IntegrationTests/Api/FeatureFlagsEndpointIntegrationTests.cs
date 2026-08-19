using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
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

    [SetUp]
    public void SetUp()
    {
        ApplicationFeatureFlags.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = true,
            SampleFeatureB = false
        });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndFlatMap_When_GetFlagsUnversioned()
    {
        var response = await _client!.GetAsync("/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        doc.RootElement.TryGetProperty("SampleFeatureA", out var a).ShouldBeTrue();
        doc.RootElement.TryGetProperty("SampleFeatureB", out var b).ShouldBeTrue();
        a.ValueKind.ShouldBe(JsonValueKind.True);
        b.ValueKind.ShouldBe(JsonValueKind.False);
    }

    [Test]
    public async Task Should_Return200AndFlatMap_When_GetFlagsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        doc.RootElement.TryGetProperty("SampleFeatureA", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("SampleFeatureB", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_HydrateFlagsFromConfiguration_When_AppStarts()
    {
        var response = await _client!.GetAsync("/api/features/flags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!["SampleFeatureA"].ShouldBeTrue();
        payload["SampleFeatureB"].ShouldBeFalse();
    }

    [Test]
    public async Task Should_RespectConfigurationOverride_When_FactoryWithInMemoryConfig()
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
        payload!["SampleFeatureA"].ShouldBeFalse();
        payload["SampleFeatureB"].ShouldBeTrue();
    }
}
