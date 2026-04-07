using System.Net;
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
public class EnvironmentStatusEndpointIntegrationTests
{
    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_MatchVersionedRoute_When_ApiVersioningUsed()
    {
        var unversioned = await _client!.GetAsync("/api/status/environment");
        var versioned = await _client.GetAsync("/api/v1.0/status/environment");

        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var a = await ReadPayloadJsonAsync(unversioned);
        var b = await ReadPayloadJsonAsync(versioned);
        a.GetRawText().ShouldBe(b.GetRawText());
    }

    [Test]
    public async Task Should_IncludeCoreRuntimeFields_When_ResponseParsed()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        root.GetProperty("osDescription").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("processorCount").GetInt32().ShouldBe(Environment.ProcessorCount);
        root.GetProperty("clrVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("hostEnvironmentName").GetString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Should_ListSelectedEnvironmentVariables_WithRedactedValues()
    {
        const string probeName = "ENV_STATUS_PROBE_SECRET";
        const string secretValue = "super-secret-value-for-redaction-probe";
        await using var factory = new EnvironmentStatusProbeWebApplicationFactory(secretValue);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain(secretValue);
        body.ShouldContain(EnvironmentStatusBuilder.RedactedValueMarker);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var variables = doc.RootElement.GetProperty("environmentVariables");
        JsonElement? probe = null;
        foreach (var entry in variables.EnumerateArray())
        {
            if (entry.GetProperty("name").GetString() == probeName)
            {
                probe = entry;
                break;
            }
        }

        probe.ShouldNotBeNull();
        probe!.Value.GetProperty("isSet").GetBoolean().ShouldBeTrue();
        probe.Value.GetProperty("value").GetString().ShouldBe(EnvironmentStatusBuilder.RedactedValueMarker);
    }

    [Test]
    public async Task Should_EnforceApiKeyPolicy_When_ConfigurationMatchesProduct()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var withoutKey = await client.GetAsync("/api/status/environment");
        withoutKey.StatusCode.ShouldBe(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var withKey = await client.GetAsync("/api/v1.0/status/environment");
        withKey.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchEtag()
    {
        using var first = await _client!.GetAsync("/api/status/environment");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/status/environment");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    private static async Task<JsonElement> ReadPayloadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.Clone();
    }

    private sealed class EnvironmentStatusProbeWebApplicationFactory(string simulatedSecret) : WebApplicationFactory<UiServerWebApplicationMarker>
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
                    ["ApiKeyAuthentication:Enabled"] = "false",
                    ["ApiKeyAuthentication:ValidationKey"] = "",
                    [EnvironmentStatusBuilder.SimulatedEnvironmentVariablesConfigurationPrefix + "ENV_STATUS_PROBE_SECRET"] = simulatedSecret
                });
            });
        }
    }
}
