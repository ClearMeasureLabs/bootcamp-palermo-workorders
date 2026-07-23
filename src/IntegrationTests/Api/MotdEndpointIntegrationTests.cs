using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class MotdEndpointIntegrationTests
{
    private const string DefaultMotdMessage = "Welcome to the AI Software Factory!";

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
    public async Task Should_Return200AndJsonMotd_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/motd");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<MotdResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe(DefaultMotdMessage);
    }

    [Test]
    public async Task Should_Return200AndJsonMotd_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/motd");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<MotdResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe(DefaultMotdMessage);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MotdAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/motd");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedPayload = await unversioned.Content.ReadFromJsonAsync<MotdResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        unversionedPayload.ShouldNotBeNull();
        unversionedPayload!.Message.ShouldNotBeNullOrEmpty();

        var versioned = await client.GetAsync("/api/v1.0/motd");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedPayload = await versioned.Content.ReadFromJsonAsync<MotdResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        versionedPayload.ShouldNotBeNull();
        versionedPayload!.Message.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Should_ReturnConfiguredMessage_When_MotdOverriddenInConfiguration()
    {
        const string overriddenMessage = "Integration test MOTD override";
        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Motd:Message"] = overriddenMessage
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/motd");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MotdResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe(overriddenMessage);
    }
}
