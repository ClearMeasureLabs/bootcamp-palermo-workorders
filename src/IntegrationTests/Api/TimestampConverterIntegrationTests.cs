using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public sealed class TimestampConverterIntegrationTests
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
    public async Task Should_ReturnJson_When_ValidEpochQuery()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?timestamp=0");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.UnixSeconds.ShouldBe(0L);
        payload.InputKind.ShouldBe("epoch_seconds");
    }

    [Test]
    public async Task Should_Return400_When_TimestampMissing()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
