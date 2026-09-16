using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsWorkOrderStatusesEndpointIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

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

    [TestCase("/api/tools/work-order-statuses")]
    [TestCase("/api/v1.0/tools/work-order-statuses")]
    public async Task Should_Return200WithStatuses_When_Get(string path)
    {
        var response = await _client!.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        statuses.ShouldNotBeNull();
        statuses.Length.ShouldBe(5);

        var keys = statuses.Select(s => s.GetProperty("key").GetString()).ToArray();
        keys.ShouldContain("Draft");
        keys.ShouldContain("Assigned");
        keys.ShouldContain("InProgress");
        keys.ShouldContain("Complete");
        keys.ShouldContain("Cancelled");
    }

    [Test]
    public async Task Should_ReturnAllRequiredFields_When_Get()
    {
        var response = await _client!.GetAsync("/api/tools/work-order-statuses");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        statuses.ShouldNotBeNull();

        foreach (var status in statuses)
        {
            status.TryGetProperty("code", out _).ShouldBeTrue();
            status.TryGetProperty("key", out _).ShouldBeTrue();
            status.TryGetProperty("friendlyName", out _).ShouldBeTrue();
            status.TryGetProperty("sortBy", out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_ReturnStatusesInSortOrder_When_Get()
    {
        var response = await _client!.GetAsync("/api/tools/work-order-statuses");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        statuses.ShouldNotBeNull();

        var sortByValues = statuses.Select(s => s.GetProperty("sortBy").GetByte()).ToList();
        sortByValues.ShouldBe(sortByValues.OrderBy(x => x).ToList());
    }
}
