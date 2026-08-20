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
    private EnvironmentStatusWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", "SQLite");
        _factory = new EnvironmentStatusWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
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

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("osDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("processorCount", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clrVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariables", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeRedactedAllowlist_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldContainKey("ASPNETCORE_ENVIRONMENT");
        payload.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"].ShouldBe(EnvironmentStatusSnapshot.RedactedValue);
        payload.EnvironmentVariables.ShouldContainKey("DATABASE_ENGINE");
        payload.EnvironmentVariables["DATABASE_ENGINE"].ShouldBe(EnvironmentStatusSnapshot.RedactedValue);
        foreach (var value in payload.EnvironmentVariables.Values)
        {
            value.ShouldBe(EnvironmentStatusSnapshot.RedactedValue);
        }
    }

    [Test]
    public async Task Should_Return401_When_CalledWithoutApiKey()
    {
        await using var factory = new EnvironmentStatusApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/status/environment");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/status/environment");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_CalledWithValidApiKey()
    {
        await using var factory = new EnvironmentStatusApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await client.GetAsync("/api/status/environment");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await client.GetAsync("/api/v1.0/status/environment");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
