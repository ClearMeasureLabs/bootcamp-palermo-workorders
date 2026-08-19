using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public async Task Should_IncludeProcessId_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("processId", out var processId).ShouldBeTrue();
        processId.GetInt32().ShouldBeGreaterThan(0);
        processId.GetInt32().ShouldBe(Environment.ProcessId);
    }

    [Test]
    public async Task Should_DeserializeProcessId_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.ProcessId.ShouldBeGreaterThan(0);
        report.ProcessId.ShouldBe(Environment.ProcessId);
    }

    [Test]
    public async Task Should_IncludeOsDescription_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("osDescription", out var osDescription).ShouldBeTrue();
        var value = osDescription.GetString();
        value.ShouldNotBeNull();
        value!.ShouldNotBeEmpty();
    }

    [Test]
    public async Task Should_DeserializeOsDescription_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.OsDescription.ShouldNotBeNull();
        report.OsDescription.ShouldNotBeEmpty();
    }

    [Test]
    public async Task Should_IncludeFrameworkDescription_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("frameworkDescription", out var frameworkDescription).ShouldBeTrue();
        var value = frameworkDescription.GetString();
        value.ShouldNotBeNull();
        value!.ShouldNotBeEmpty();
    }

    [Test]
    public async Task Should_DeserializeFrameworkDescription_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.FrameworkDescription.ShouldNotBeNull();
        report.FrameworkDescription.ShouldNotBeEmpty();
    }

    [Test]
    public async Task Should_IncludeGcMemoryMb_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("gcMemoryMb", out var gcMemoryMb).ShouldBeTrue();
        gcMemoryMb.ValueKind.ShouldBe(JsonValueKind.Number);
        gcMemoryMb.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_DeserializeGcMemoryMb_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_IncludeWorkingSetMb_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("workingSetMb", out var workingSetMb).ShouldBeTrue();
        workingSetMb.ValueKind.ShouldBe(JsonValueKind.Number);
        workingSetMb.GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_DeserializeWorkingSetMb_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_IncludeProcessorCount_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("processorCount", out var processorCount).ShouldBeTrue();
        processorCount.ValueKind.ShouldBe(JsonValueKind.Number);
        processorCount.GetInt32().ShouldBeGreaterThanOrEqualTo(1);
        processorCount.GetInt32().ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public async Task Should_DeserializeProcessorCount_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.ProcessorCount.ShouldBeGreaterThanOrEqualTo(1);
        report.ProcessorCount.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public async Task Should_IncludeIs64BitProcess_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("is64BitProcess", out var is64BitProcess).ShouldBeTrue();
        is64BitProcess.GetBoolean().ShouldBe(Environment.Is64BitProcess);
    }

    [Test]
    public async Task Should_DeserializeIs64BitProcess_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.Is64BitProcess.ShouldBe(Environment.Is64BitProcess);
    }

    [Test]
    public async Task GetApiHealthDetailedReturnsTimeZoneId()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("timeZoneId", out var timeZoneId).ShouldBeTrue();
        timeZoneId.GetString().ShouldBe(TimeZoneInfo.Local.Id);
    }

    [Test]
    public async Task Should_IncludeProcessPriority_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("processPriority", out var processPriority).ShouldBeTrue();
        processPriority.ValueKind.ShouldBe(JsonValueKind.String);
        var value = processPriority.GetString();
        value.ShouldNotBeNull();
        value!.ShouldNotBeEmpty();
        value.ShouldBe(Process.GetCurrentProcess().PriorityClass.ToString());
    }

    [Test]
    public async Task Should_DeserializeProcessPriority_ToDetailedHealthReport_When_GetDetailedHealth()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        report.ShouldNotBeNull();
        report!.ProcessPriority.ShouldNotBeNull();
        report.ProcessPriority.ShouldNotBeEmpty();
        report.ProcessPriority.ShouldBe(Process.GetCurrentProcess().PriorityClass.ToString());
    }

    [Test]
    public async Task Should_SupportVersionedRoute_When_GetApiV1HealthDetailed()
    {
        var legacy = await _client!.GetAsync("/api/health/detailed");
        var v1 = await _client.GetAsync("/api/v1.0/health/detailed");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        var legacyReport = await legacy.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var v1Report = await v1.Content.ReadFromJsonAsync<DetailedHealthReport>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        legacyReport.ShouldNotBeNull();
        v1Report.ShouldNotBeNull();
        v1Report!.OverallStatus.ShouldBe(legacyReport!.OverallStatus);
        v1Report.Components.Count.ShouldBe(legacyReport.Components.Count);
    }

    [Test]
    public async Task Should_SetEtagHeader_When_DetailedHealthReturned()
    {
        var response = await _client!.GetAsync("/api/health/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag!.ToString().ShouldStartWith("W/\"");
    }

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchMatchesDetailedHealthEtag()
    {
        await using var factory = new FixedDetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();
        var first = await client.GetAsync("/api/health/detailed");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/health/detailed");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    [Test]
    public async Task Should_Return200WithPayload_When_IfNoneMatchDiffersFromDetailedHealthEtag()
    {
        await using var factory = new FixedDetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();
        var first = await client.GetAsync("/api/health/detailed");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/health/detailed");
        second.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue("\"stale\""));
        var response = await client.SendAsync(second);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Length.ShouldBeGreaterThan(0);
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
        names.ShouldContain("NeedsReboot");
        names.ShouldContain("ProcessThreadCount");
        foreach (var c in report.Components)
        {
            (c.Status == ComponentHealthStatus.Healthy
                || c.Status == ComponentHealthStatus.Degraded
                || c.Status == ComponentHealthStatus.Unhealthy).ShouldBeTrue();
        }
    }

    private sealed class FixedDetailedHealthWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
    {
        private static readonly DetailedHealthReport FixedReport = new()
        {
            OverallStatus = ComponentHealthStatus.Healthy,
            CheckedAtUtc = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc),
            ProcessId = 1,
            OsDescription = "Test OS",
            FrameworkDescription = ".NET Test",
            GcMemoryMb = 10,
            WorkingSetMb = 20,
            ProcessorCount = 4,
            Is64BitProcess = true,
            TimeZoneId = "UTC",
            ProcessPriority = "Normal",
            Components =
            [
                new ComponentHealthEntry { Name = "API", Status = ComponentHealthStatus.Healthy }
            ]
        };

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
                    ["FeatureFlags:SampleFeatureA"] = "false",
                    ["FeatureFlags:SampleFeatureB"] = "false"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDetailedHealthReportProvider>();
                services.AddSingleton<IDetailedHealthReportProvider, StubDetailedHealthReportProvider>();
            });
        }

        private sealed class StubDetailedHealthReportProvider : IDetailedHealthReportProvider
        {
            public Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(FixedReport);
        }
    }
}
