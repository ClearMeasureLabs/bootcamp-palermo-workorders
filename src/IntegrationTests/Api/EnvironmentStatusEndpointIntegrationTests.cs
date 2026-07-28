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
    public async Task Should_Return200AndJson_When_GetUnversioned()
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
        doc.RootElement.TryGetProperty("environmentVariables", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("osDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("processorCount", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clrVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariables", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeRuntimeMetadata_FromHost_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldNotBeNullOrWhiteSpace();
        payload.ProcessorCount.ShouldBeGreaterThanOrEqualTo(1);
        payload.ClrVersion.ShouldNotBeNullOrWhiteSpace();
        payload.ClrVersion.ShouldContain(Environment.Version.Major.ToString());
    }

    [Test]
    public async Task Should_RedactSensitiveValues_When_ConfigurationContainsSecrets()
    {
        const string connectionString = "Server=integration-secret-host;Password=integration-secret-pwd";
        const string openAiKey = "sk-integration-test-secret";
        const string appInsights = "InstrumentationKey=integration-secret-key";
        const string validationKey = "integration-validation-secret";

        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SqlConnectionString"] = connectionString,
                    ["AI_OpenAI_ApiKey"] = openAiKey,
                    ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = appInsights,
                    ["ApiKeyAuthentication:ValidationKey"] = validationKey
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(connectionString);
        body.ShouldNotContain(openAiKey);
        body.ShouldNotContain(appInsights);
        body.ShouldNotContain(validationKey);

        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        foreach (var value in payload!.EnvironmentVariables.Values)
        {
            value.ShouldBe(EnvironmentStatusResponseBuilder.RedactedEnvironmentVariableValue);
        }
    }

    [Test]
    public async Task Should_IncludeAllCuratedKeys_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.Count.ShouldBe(10);
        payload.EnvironmentVariables.ShouldContainKey("ASPNETCORE_ENVIRONMENT");
        payload.EnvironmentVariables.ShouldContainKey("DATABASE_ENGINE");
        payload.EnvironmentVariables.ShouldContainKey("ConnectionStrings__SqlConnectionString");
        payload.EnvironmentVariables.ShouldContainKey("APPLICATIONINSIGHTS_CONNECTION_STRING");
        payload.EnvironmentVariables.ShouldContainKey("AI_OpenAI_ApiKey");
        payload.EnvironmentVariables.ShouldContainKey("AI_OpenAI_Url");
        payload.EnvironmentVariables.ShouldContainKey("AI_OpenAI_Model");
        payload.EnvironmentVariables.ShouldContainKey("ApiKeyAuthentication__Enabled");
        payload.EnvironmentVariables.ShouldContainKey("ApiKeyAuthentication__ValidationKey");
        payload.EnvironmentVariables.ShouldContainKey("OTEL_EXPORTER_OTLP_ENDPOINT");
        foreach (var value in payload.EnvironmentVariables.Values)
        {
            value.ShouldBe(EnvironmentStatusResponseBuilder.RedactedEnvironmentVariableValue);
        }
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
}
