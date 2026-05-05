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
public class EnvironmentStatusEndpointIntegrationTests
{
    private const string DistinctiveSecretEnvName = "BOOTCAMP_ENV_STATUS_INTEG_SECRET_6191";
    private const string DistinctiveSecretValue = "integration-distinctive-6191";

    private EnvironmentStatusWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new EnvironmentStatusWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        Environment.SetEnvironmentVariable(DistinctiveSecretEnvName, null);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatusUnversioned()
    {
        var response = await _client!.GetAsync("/api/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("osDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("processorCount", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clrVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariables", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatusVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_ParsePayload_WithProcessorCountNonNegative_When_ResponseDeserialized()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<RuntimeEnvironmentResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.ProcessorCount.ShouldBeGreaterThanOrEqualTo(0);
        payload.OsDescription.ShouldNotBeNullOrWhiteSpace();
        payload.ClrVersion.ShouldNotBeNullOrWhiteSpace();
        payload.FrameworkDescription.ShouldNotBeNullOrWhiteSpace();
        payload.EnvironmentVariables.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_ReflectConfiguredAllowlist_When_RuntimeEnvironmentOptionsOverridden()
    {
        await using var factory = new EnvironmentStatusWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RuntimeEnvironmentStatus:VariableNames:0"] = "ASPNETCORE_ENVIRONMENT",
                    ["RuntimeEnvironmentStatus:VariableNames:1"] = "TEST_VAR_6191_INTEG_A",
                    ["RuntimeEnvironmentStatus:VariableNames:2"] = "TEST_VAR_6191_INTEG_B"
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<RuntimeEnvironmentResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.Select(e => e.Name).ToList().ShouldBe(
            new List<string> { "ASPNETCORE_ENVIRONMENT", "TEST_VAR_6191_INTEG_A", "TEST_VAR_6191_INTEG_B" });
    }

    [Test]
    public async Task Should_Redact_When_ProcessEnvContainsDistinctiveValue()
    {
        Environment.SetEnvironmentVariable(DistinctiveSecretEnvName, DistinctiveSecretValue);
        await using var factory = new EnvironmentStatusWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RuntimeEnvironmentStatus:VariableNames:0"] = DistinctiveSecretEnvName
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(DistinctiveSecretValue);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/status/environment");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/status/environment");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/status/environment");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/status/environment");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_NotConflictWithAdjacentRoutes_When_Registered()
    {
        var env = await _client!.GetAsync("/api/status/environment");
        env.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ping = await _client.GetAsync("/api/ping");
        ping.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
