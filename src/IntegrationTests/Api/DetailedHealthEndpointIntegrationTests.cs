using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class DetailedHealthEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_GetSimpleHealth()
    {
        var response = await _client!.GetAsync("/api/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("status", out var status).ShouldBeTrue();
        status.GetString().ShouldBe(SimpleHealthStatus.Healthy);
        doc.RootElement.TryGetProperty("currentTimeUtc", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_GetSimpleHealth()
    {
        using var anonymous = _factory!.CreateClient();
        var response = await anonymous.GetAsync("/api/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnRecentUtcAndNonNegativeUptime_When_GetSimpleHealth()
    {
        var response = await _client!.GetAsync("/api/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SimpleHealthResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.CurrentTimeUtc.Kind.ShouldBe(DateTimeKind.Utc);
        (DateTime.UtcNow - payload.CurrentTimeUtc).Duration().ShouldBeLessThan(TimeSpan.FromMinutes(5));
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("overallStatus", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("checkedAtUtc", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("components", out var components).ShouldBeTrue();
        components.ValueKind.ShouldBe(JsonValueKind.Array);
        components.GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_ExposeOverallStatus_WorstCase_When_ComponentsMixed()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.OverallStatus.ShouldBe(HealthReportBuilder.AggregateWorst(report.Components));
    }

    [Test]
    public async Task Should_IncludeCheckedAtUtc_ParseableUtc_When_ResponseReturned()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.CheckedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        (DateTime.UtcNow - report.CheckedAtUtc).Duration().ShouldBeLessThan(TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_NoAuthHeaders()
    {
        using var anonymous = _factory!.CreateClient();
        var response = await anonymous.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ListExpectedComponentEntries_When_MockPayload()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        var names = report!.Components.Select(c => c.Name).ToHashSet();
        names.ShouldContain("LlmGateway");
        names.ShouldContain("DataAccess");
        names.ShouldContain("Server");
        names.ShouldContain("API");
        names.ShouldContain("Jeffrey");
        foreach (var c in report.Components)
        {
            (c.Status == ComponentHealthStatus.Healthy
                || c.Status == ComponentHealthStatus.Degraded
                || c.Status == ComponentHealthStatus.Unhealthy).ShouldBeTrue();
        }
    }
}
