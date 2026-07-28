using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
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

    [TestCase("/api/tools/timestamp-converter")]
    [TestCase("/api/v1.0/tools/timestamp-converter")]
    public async Task Should_Return200AndParsedTimestamp_When_UnixSeconds(string route)
    {
        var response = await _client!.GetAsync($"{route}?unix=1609459200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        doc.RootElement.GetProperty("unixSeconds").GetInt64().ShouldBe(1609459200L);
        doc.RootElement.GetProperty("utcIso8601").GetString().ShouldBe("2021-01-01T00:00:00.0000000Z");
    }

    [TestCase("/api/tools/timestamp-converter")]
    [TestCase("/api/v1.0/tools/timestamp-converter")]
    public async Task Should_Return200_When_UnixMilliseconds(string route)
    {
        var response = await _client!.GetAsync($"{route}?unix=1609459200000");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.UnixMilliseconds.ShouldBe(1609459200000L);
    }

    [Test]
    public async Task Should_UseSecondsVersusMillisecondsHeuristic_When_InputScaleDiffersButInstantCanMatch()
    {
        var asSeconds = await _client!.GetAsync("/api/tools/timestamp-converter?unix=1700000000");
        asSeconds.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondsPayload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            await asSeconds.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions)!;

        var asMilliseconds =
            await _client.GetAsync("/api/tools/timestamp-converter?unix=1700000000000");
        asMilliseconds.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonSerializer.Deserialize<TimestampConverterResponse>(
                await asMilliseconds.Content.ReadAsStringAsync(),
                ConditionalGetEtag.JsonSerializerOptions)!
            .UtcIso8601.ShouldBe(secondsPayload.UtcIso8601);

        var outOfRangeSeconds = await _client.GetAsync("/api/tools/timestamp-converter?unix=253402400000");
        outOfRangeSeconds.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var fractionalMsDifference = await _client.GetAsync("/api/tools/timestamp-converter?unix=1000000000001");
        fractionalMsDifference.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonSerializer.Deserialize<TimestampConverterResponse>(
                await fractionalMsDifference.Content.ReadAsStringAsync(),
                ConditionalGetEtag.JsonSerializerOptions)!
            .UtcIso8601.ShouldBe("2001-09-09T01:46:40.0010000Z");
    }

    [Test]
    public async Task Should_Return200_When_ValidIso()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?iso=2026-03-30T12%3A00%3A00.0000000Z");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.UtcIso8601.ShouldBe("2026-03-30T12:00:00.0000000Z");
        payload.UnixSeconds.ShouldBe(new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    [TestCase("/api/tools/timestamp-converter")]
    [TestCase("/api/v1.0/tools/timestamp-converter")]
    public async Task Should_Return400_When_BothParameters(string route)
    {
        var response = await _client!.GetAsync($"{route}?iso=1970-01-01Z&unix=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_MissingBothParameters()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_GarbageIso()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=zzz");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return304_When_EtagMatchesIfNoneMatch()
    {
        var first = await _client!.GetAsync("/api/tools/timestamp-converter?unix=0");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.ETag.ShouldNotBeNull();
        using var rq = new HttpRequestMessage(HttpMethod.Get, "/api/tools/timestamp-converter?unix=0");
        rq.Headers.IfNoneMatch.Add(first.Headers.ETag!);

        var second = await _client.SendAsync(rq);

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }
}
