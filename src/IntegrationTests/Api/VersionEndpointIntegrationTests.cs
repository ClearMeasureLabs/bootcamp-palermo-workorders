using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.Data.Sqlite;
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
    public async Task Should_Return200WithAllRequiredFields_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        response.Headers.ETag.ShouldNotBeNull();

        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            await response.Content.ReadAsStringAsync(),
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.InformationalVersion.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.Environment.ShouldNotBeNullOrEmpty();
        payload.MachineName.ShouldNotBeNullOrEmpty();
        payload.FrameworkDescription.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var first = await _client!.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/version");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Test]
    public async Task Should_Return429TooManyRequests_When_RateLimitExceeded()
    {
        await using var hold = new SqliteConnection(GrpcWebApplicationFactory.RateLimitTestSqliteConnectionString);
        await hold.OpenAsync();
        await using var rateLimitedFactory =
            new RateLimitedApiWebApplicationFactory(GrpcWebApplicationFactory.RateLimitTestSqliteConnectionString);
        var http = rateLimitedFactory.CreateClient();

        (await http.GetAsync("/api/version")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await http.GetAsync("/api/version")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
