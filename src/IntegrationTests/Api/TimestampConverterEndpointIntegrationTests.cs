using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task Should_Return200AndJson_When_GetTimestampConverterUnversioned_WithValidEpoch()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1704067200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertCanonicalPayload(response);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetTimestampConverterVersioned_WithValidEpoch()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1704067200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertCanonicalPayload(response);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetTimestampConverter_WithValidIso()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=2024-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertCanonicalPayload(response);
    }

    [Test]
    public async Task Should_Return400_When_BothParametersProvided()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?epoch=1704067200&iso=2024-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        problem.ShouldNotBeNull();
        problem!.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("exactly one");
    }

    [Test]
    public async Task Should_Return400_When_NoParametersProvided()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        problem.ShouldNotBeNull();
        problem!.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("required");
    }

    [Test]
    public async Task Should_Return400_When_InvalidEpoch()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=not-a-number");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        problem.ShouldNotBeNull();
        problem!.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("integer");
    }

    [Test]
    public async Task Should_Return400_When_InvalidIso()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=invalid-date");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        problem.ShouldNotBeNull();
        problem!.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("ISO-8601");
    }

    [Test]
    public async Task Should_Return200AndValidETag_When_SameRequestTwice()
    {
        var first = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1704067200");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var secondRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/tools/timestamp-converter?epoch=1704067200");
        secondRequest.Headers.IfNoneMatch.Add(etag!);

        var second = await _client.SendAsync(secondRequest);
        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await second.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/tools/timestamp-converter?epoch=1704067200");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/tools/timestamp-converter?epoch=1704067200");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task AssertCanonicalPayload(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("epochSeconds").GetInt64().ShouldBe(1704067200L);
        doc.RootElement.GetProperty("epochMilliseconds").GetInt64().ShouldBe(1704067200000L);
        doc.RootElement.GetProperty("iso8601Utc").GetString().ShouldBe("2024-01-01T00:00:00.0000000Z");
        doc.RootElement.GetProperty("utcDisplay").GetString().ShouldNotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("localDisplay").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private sealed class ProblemDetailsPayload
    {
        public string? Detail { get; set; }
    }
}
