using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
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

    [SetUp]
    public async Task SetUp()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
        await _client!.GetAsync("/_demo/setneedsreboot/false");
    }

    [TearDown]
    public async Task TearDown()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
        await _client!.GetAsync("/_demo/setneedsreboot/false");
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
    public async Task Should_ListExpectedComponentEntries_When_AggregatedFromRegisteredChecks()
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

    [Test]
    public async Task Should_Return200AndSamePayloadShape_When_LegacyAndV1DetailedPaths()
    {
        var legacy = await _client!.GetAsync("/api/health/detailed");
        var v1 = await _client.GetAsync("/api/v1.0/health/detailed");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.Headers.TryGetValues("api-supported-versions", out var versions).ShouldBeTrue();
        versions.ShouldNotBeNull();
        string.Join(", ", versions!).ShouldContain("1.0");

        var legacyReport = await legacy.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var v1Report = await v1.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        legacyReport.ShouldNotBeNull();
        v1Report.ShouldNotBeNull();
        legacyReport!.OverallStatus.ShouldBe(v1Report!.OverallStatus);
        legacyReport.Components.Select(c => c.Name).ToHashSet()
            .ShouldBe(v1Report.Components.Select(c => c.Name).ToHashSet());
    }

    [Test]
    public async Task Should_Return200WithUnhealthyOverallStatus_When_ComponentFails()
    {
        var setResponse = await _client!.GetAsync("/_demo/setneedsreboot/true");
        setResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await _client.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.OverallStatus.ShouldBe(ComponentHealthStatus.Unhealthy);
        var needsReboot = report.Components.Single(c => c.Name == "NeedsReboot");
        needsReboot.Status.ShouldBe(ComponentHealthStatus.Unhealthy);
    }

    [Test]
    public async Task Should_ExcludeLiveTaggedChecks_When_BuildingDetailedReport()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        var names = report!.Components.Select(c => c.Name).ToHashSet();
        names.ShouldNotContain("self");
        names.ShouldContain("NeedsReboot");
    }

    [Test]
    public async Task Should_SerializeCamelCaseProperties_When_DetailedHealthReturned()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("overallStatus", out _).ShouldBeTrue();
        root.TryGetProperty("checkedAtUtc", out _).ShouldBeTrue();
        root.TryGetProperty("components", out var components).ShouldBeTrue();
        root.TryGetProperty("OverallStatus", out _).ShouldBeFalse();
        root.TryGetProperty("CheckedAtUtc", out _).ShouldBeFalse();

        var first = components.EnumerateArray().First();
        first.TryGetProperty("name", out _).ShouldBeTrue();
        first.TryGetProperty("status", out _).ShouldBeTrue();
        first.TryGetProperty("durationMs", out _).ShouldBeTrue();
        first.TryGetProperty("Name", out _).ShouldBeFalse();
        first.TryGetProperty("DurationMs", out _).ShouldBeFalse();

        foreach (var component in components.EnumerateArray())
        {
            foreach (var property in component.EnumerateObject())
            {
                char.IsUpper(property.Name[0]).ShouldBeFalse(
                    $"Property '{property.Name}' should be camelCase");
            }
        }
    }
}
