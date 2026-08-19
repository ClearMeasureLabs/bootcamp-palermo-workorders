using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class TimestampConverterEndpointIntegrationTests
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
    public async Task Should_Return200WithJsonShape_When_GetUnversionedWithEpochSeconds()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1609459200");

        await AssertSuccessPayload(response);
    }

    [Test]
    public async Task Should_Return200WithJsonShape_When_GetVersionedWithEpochSeconds()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1609459200");

        await AssertSuccessPayload(response);
    }

    [Test]
    public async Task Should_Return200WithJsonShape_When_GetWithIso8601()
    {
        var unversioned = await _client!.GetAsync("/api/tools/timestamp-converter?iso=2021-01-01T00:00:00Z");
        await AssertSuccessPayload(unversioned);

        var versioned = await _client.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2021-01-01T00:00:00Z");
        await AssertSuccessPayload(versioned);
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_MissingBothParams()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        await AssertProblem400(response);
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_BothParamsSupplied()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?epoch=1234&iso=2021-01-01T00:00:00Z");

        await AssertProblem400(response);
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_InvalidEpochFormat()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=abc");

        await AssertProblem400(response);
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_InvalidIsoFormat()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=not-a-date");

        await AssertProblem400(response);
    }

    [Test]
    public async Task Should_AllowAnonymous_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/timestamp-converter?epoch=1609459200");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1609459200");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task AssertSuccessPayload(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<TimestampConverterResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldNotBeNullOrWhiteSpace();
        payload.UtcFormatted.ShouldNotBeNullOrWhiteSpace();
        payload.LocalFormatted.ShouldNotBeNullOrWhiteSpace();
    }

    private static async Task AssertProblem400(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("detail", out var detail).ShouldBeTrue();
        detail.GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
