using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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
        doc.RootElement.TryGetProperty("hostEnvironmentName", out _).ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("hostEnvironmentName", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariables", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeAspNetCoreEnvironment_FromHost()
    {
        var response = await _client!.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.HostEnvironmentName.ShouldBe("Testing");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/status/environment");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/status/environment");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_SupportConditionalGet_When_IfNoneMatchSent()
    {
        var first = await _client!.GetAsync("/api/status/environment");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/status/environment");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    [Test]
    public async Task Should_RedactConfiguredEnvironmentVariables_When_SeededInProcess()
    {
        const string secret = "integration-test-secret-value";
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", secret);
        try
        {
            var response = await _client!.GetAsync("/api/status/environment");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.ShouldNotContain(secret);
            body.ShouldContain(EnvironmentStatusBuilder.RedactedValue);

            var payload = await response.Content.ReadFromJsonAsync<EnvironmentStatusResponse>(
                ConditionalGetEtag.JsonSerializerOptions);
            payload.ShouldNotBeNull();
            payload!.EnvironmentVariables.ContainsKey("DATABASE_ENGINE").ShouldBeTrue();
            payload.EnvironmentVariables["DATABASE_ENGINE"].ShouldBe(EnvironmentStatusBuilder.RedactedValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_ENGINE", null);
        }
    }
}
