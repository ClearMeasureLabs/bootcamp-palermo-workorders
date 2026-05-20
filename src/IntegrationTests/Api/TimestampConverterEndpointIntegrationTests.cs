using System.Net;
using System.Text.Json;
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
    public async Task Should_Return200AndJson_When_ValidUnixEpochSeconds_Supplied()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1774872000");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var doc = await ParseJsonAsync(response);
        doc.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(1774872000);
        doc.RootElement.GetProperty("iso8601Utc").GetString().ShouldNotBeNull();
        doc.RootElement.GetProperty("iso8601Utc").GetString()!.ShouldContain("2026-03-30");
    }

    [Test]
    public async Task Should_Return200AndJson_When_ValidUnixEpochMilliseconds_Supplied()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1774872000000");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = await ParseJsonAsync(response);
        doc.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(1774872000);
    }

    [Test]
    public async Task Should_Return200AndJson_When_ValidIso8601_Supplied()
    {
        var encoded = Uri.EscapeDataString("2026-03-30T12:00:00Z");
        var response = await _client!.GetAsync($"/api/tools/timestamp-converter?iso={encoded}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = await ParseJsonAsync(response);
        doc.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(1774872000);
    }

    [Test]
    public async Task Should_Return200_When_VersionedRoute_Supplied()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = await ParseJsonAsync(response);
        doc.RootElement.GetProperty("unixEpochSeconds").GetInt64().ShouldBe(0);
    }

    [Test]
    public async Task Should_Return400Problem_When_InputMissing()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var ct = response.Content.Headers.ContentType;
        ct.ShouldNotBeNull();
        ct!.MediaType.ShouldNotBeNull();
        ct.MediaType.ShouldContain("application/problem");
    }

    [Test]
    public async Task Should_Return400Problem_When_BothEpochAndIso_Supplied()
    {
        var encoded = Uri.EscapeDataString("2026-03-30T12:00:00Z");
        var response = await _client!.GetAsync(
            $"/api/tools/timestamp-converter?epoch=0&iso={encoded}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400Problem_When_UnparseableIso()
    {
        var encoded = Uri.EscapeDataString("not-valid");
        var response = await _client!.GetAsync($"/api/tools/timestamp-converter?iso={encoded}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400Problem_When_EpochOutOfRange()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?epoch=9223372036854775807");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ReuseDiagnosticsApiKeyBypass_When_TimestampConverterEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();

        using (var anon = factory.CreateClient())
        {
            var ok = await anon.GetAsync("/api/tools/timestamp-converter?epoch=0");
            ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var protectedClient = factory.CreateClient();
        var diag = await protectedClient.GetAsync("/api/diagnostics");
        diag.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var diagnosticsOk = await withKey.GetAsync("/api/diagnostics");
        diagnosticsOk.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
