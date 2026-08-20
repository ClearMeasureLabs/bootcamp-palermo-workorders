using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
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
    public async Task Should_Return200AndJson_When_GetEnvironmentUnversioned()
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
    public async Task Should_Return200AndJson_When_GetEnvironmentVersioned()
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
    public async Task Should_ExposeRedactedAllowlist_When_EnvVarsConfiguredInFactory()
    {
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", "SQLite");
        Environment.SetEnvironmentVariable("AI_OpenAI_ApiKey", "sk-factory-secret-key");
        try
        {
            await using var factory = new DiagnosticsWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/status/environment");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.ShouldNotContain("sk-factory-secret-key");

            var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            payload.ShouldNotBeNull();
            payload!.EnvironmentVariables.ShouldContain(e =>
                e.Name == "DATABASE_ENGINE"
                && e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue);
            payload.EnvironmentVariables.ShouldContain(e =>
                e.Name == "AI_OpenAI_ApiKey"
                && e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue);
            payload.EnvironmentVariables.All(e => e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue).ShouldBeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_ENGINE", null);
            Environment.SetEnvironmentVariable("AI_OpenAI_ApiKey", null);
        }
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndEnvironmentProtected()
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
