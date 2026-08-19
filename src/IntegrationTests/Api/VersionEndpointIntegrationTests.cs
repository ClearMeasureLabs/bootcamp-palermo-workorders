using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class VersionEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_GetVersionUnversioned()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("assemblyVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("informationalVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("buildConfiguration", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("machineName", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersionVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("assemblyVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("informationalVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("buildConfiguration", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("machineName", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_IncludeBuildConfiguration_In_Response()
    {
        var response = await _client!.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await DeserializeVersionPayload(response);
        payload.ShouldNotBeNull();
        payload!.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldBeOneOf("Debug", "Release", "exclude-maui.slnf");
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_NoApiKeyRequired()
    {
        var response = await _client!.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_SetWeakEtag_On_Response()
    {
        var response = await _client!.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var etag = response.Headers.ETag;
        etag.ShouldNotBeNull();
        etag!.Tag.ToString().ShouldStartWith("\"");
        etag.Tag.ToString().ShouldEndWith("\"");
        etag.IsWeak.ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return304_When_IfNoneMatchIncludesEtag()
    {
        var first = await _client!.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/version");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    [Test]
    public async Task Should_ApplyOutputCachePolicy_When_VersionMetadataRequested()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        var second = await client.GetAsync("/api/version");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldNotBeNull();
        second.Headers.Age!.Value.TotalSeconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    private static async Task<VersionMetadataResponse?> DeserializeVersionPayload(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<VersionMetadataResponse>(
            stream,
            ConditionalGetEtag.JsonSerializerOptions);
    }
}
