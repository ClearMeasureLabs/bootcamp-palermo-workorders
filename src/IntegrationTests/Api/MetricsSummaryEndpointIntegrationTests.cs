using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class MetricsSummaryEndpointIntegrationTests
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
    public async Task Should_return_200_for_get_unversioned_route()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_return_200_for_get_versioned_route()
    {
        var response = await _client!.GetAsync("/api/v1.0/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_return_valid_json_schema()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out var totalRequests).ShouldBeTrue();
        root.TryGetProperty("gcHeapMemoryMb", out var gcHeap).ShouldBeTrue();
        root.TryGetProperty("workingSetMb", out var workingSet).ShouldBeTrue();
        root.TryGetProperty("gcGen0Collections", out var gen0).ShouldBeTrue();
        root.TryGetProperty("gcGen1Collections", out var gen1).ShouldBeTrue();
        root.TryGetProperty("gcGen2Collections", out var gen2).ShouldBeTrue();

        totalRequests.GetInt64().ShouldBeGreaterThanOrEqualTo(0);
        gcHeap.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
        workingSet.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
        gen0.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
        gen1.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
        gen2.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_increment_request_count_after_warm_up_gets()
    {
        long previous = 0;
        for (var i = 0; i < 5; i++)
        {
            var response = await _client!.GetAsync("/api/metrics/summary");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            payload.ShouldNotBeNull();
            payload!.TotalRequestsServed.ShouldBeGreaterThan(previous);
            previous = payload.TotalRequestsServed;
        }
    }

    [Test]
    public async Task Should_enforce_api_key_authentication()
    {
        await using var factory = new MetricsSummaryApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/metrics/summary");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/metrics/summary");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/metrics/summary");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

/// <summary>
/// Hosts UI.Server with API-key authentication enabled for metrics summary tests.
/// </summary>
public sealed class MetricsSummaryApiKeyProtectedWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "true",
                ["ApiKeyAuthentication:ValidationKey"] = ApiKeyProtectedWebApplicationFactory.TestApiKey
            });
        });
    }
}
