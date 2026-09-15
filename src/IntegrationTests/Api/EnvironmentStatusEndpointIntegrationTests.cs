using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EnvironmentStatusEndpointIntegrationTests
{
    private const string SecretValue = "integration-env-status-secret-value";
    private DiagnosticsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.SetEnvironmentVariable(
            EnvironmentStatusController.RedactionProbeVariableName, SecretValue);
        _factory = new DiagnosticsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        Environment.SetEnvironmentVariable(
            EnvironmentStatusController.RedactionProbeVariableName, null);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatusUnversioned()
    {
        var response = await _client!.GetAsync("/api/status/environment");

        await AssertOkJsonShape(response);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatusVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/status/environment");

        await AssertOkJsonShape(response);
    }

    [Test]
    public async Task Should_ExposeRuntimeFields_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ProcessorCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_ExposeEnvVarNamesWithoutValues_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotContain(SecretValue);

        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            body, ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.EnvironmentVariableNames.ShouldContain(
            EnvironmentStatusController.RedactionProbeVariableName);
        payload.EnvironmentVariables[EnvironmentStatusController.RedactionProbeVariableName]
            .ShouldBe(EnvironmentStatusController.RedactedValue);
    }

    [Test]
    public async Task Should_Return401_When_ApiKeyRequiredAndMissing()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/status/environment");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/status/environment");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_ApiKeyRequiredAndValid()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/status/environment");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = ok.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        var okVersioned = await withKey.GetAsync("/api/v1.0/status/environment");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task AssertOkJsonShape(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("osDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("processorCount", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clrVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariableNames", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariables", out _).ShouldBeTrue();
    }
}
