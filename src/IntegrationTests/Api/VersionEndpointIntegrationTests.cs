using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
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
    public async Task Should_Return200AndJson_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeVersionPayload(response);
        AssertRequiredFields(payload);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeVersionPayload(response);
        AssertRequiredFields(payload);
    }

    [Test]
    public async Task Should_ReturnEquivalentPayload_When_UnversionedAndVersionedPaths()
    {
        var unversioned = await DeserializeVersionPayload(await _client!.GetAsync("/api/version"));
        var versioned = await DeserializeVersionPayload(await _client.GetAsync("/api/v1.0/version"));

        unversioned.AssemblyVersion.ShouldBe(versioned.AssemblyVersion);
        unversioned.InformationalVersion.ShouldBe(versioned.InformationalVersion);
        unversioned.BuildConfiguration.ShouldBe(versioned.BuildConfiguration);
        unversioned.Environment.ShouldBe(versioned.Environment);
        unversioned.MachineName.ShouldBe(versioned.MachineName);
        unversioned.FrameworkDescription.ShouldBe(versioned.FrameworkDescription);
    }

    [Test]
    public async Task Should_ExposeTestingEnvironment_When_HostUsesTestingEnvironment()
    {
        var payload = await DeserializeVersionPayload(await _client!.GetAsync("/api/version"));

        payload.Environment.ShouldBe("Testing");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_VersionAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertRequiredFields(await DeserializeVersionPayload(unversioned));

        var versioned = await client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertRequiredFields(await DeserializeVersionPayload(versioned));
    }

    [Test]
    public async Task Should_IncludeSupportedVersionsHeader_When_GetVersionedRoute()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("api-supported-versions", out var values).ShouldBeTrue();
        values.ShouldNotBeNull();
        string.Join(", ", values!).ShouldContain("1.0");
    }

    [Test]
    public async Task Should_IncludeEtagHeader_When_GetVersion()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
    }

    private static async Task<VersionMetadataResponse> DeserializeVersionPayload(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            json,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }

    private static void AssertRequiredFields(VersionMetadataResponse payload)
    {
        payload.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.InformationalVersion.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.Environment.ShouldNotBeNullOrEmpty();
        payload.MachineName.ShouldNotBeNullOrEmpty();
        payload.FrameworkDescription.ShouldNotBeNullOrEmpty();
    }
}
