using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.Configuration;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EnvironmentStatusEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_GetEnvironmentStatus()
    {
        var response = await _client!.GetAsync("/api/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<EnvironmentStatusResponse>(
            stream,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.OsDescription.ShouldNotBeNullOrWhiteSpace();
        payload.ProcessorCount.ShouldBeGreaterThan(0);
        payload.ClrVersion.ShouldNotBeNullOrWhiteSpace();
        payload.EnvironmentVariables.Count.ShouldBe(EnvironmentStatusResponseBuilder.DiagnosticEnvironmentVariableNames.Length);
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_ApiKeyEnabled()
    {
        using var factory = new DetailedHealthWebApplicationFactory();
        factory.WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiKeyAuthentication:Enabled"] = "true",
                    ["ApiKeyAuthentication:ValidationKey"] = "secret-key"
                });
            });
        });

        using var anonymous = factory.CreateClient();
        var response = await anonymous.GetAsync("/api/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
