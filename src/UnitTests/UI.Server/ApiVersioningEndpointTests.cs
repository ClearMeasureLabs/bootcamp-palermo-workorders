using System.Net;
using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiVersioningEndpointTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiVersioningRoutingWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndSamePayload_When_GetSimpleHealth_LegacyAndV1Paths()
    {
        var legacy = await _client!.GetAsync("/api/health");
        var v1 = await _client.GetAsync("/api/v1.0/health");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var legacyDoc = JsonDocument.Parse(await legacy.Content.ReadAsStringAsync());
        using var v1Doc = JsonDocument.Parse(await v1.Content.ReadAsStringAsync());
        legacyDoc.RootElement.GetProperty("status").GetString().ShouldBe(v1Doc.RootElement.GetProperty("status").GetString());
        legacyDoc.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    [Test]
    public async Task Should_ReturnNotSuccess_When_GetSimpleHealth_UnsupportedVersion()
    {
        var response = await _client!.GetAsync("/api/v2.0/health");

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200_When_GetVersion_V1Path()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_Return200_With_BuildVersion_CommitHash_On_LegacyPath()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertVersionMetadataFields(document.RootElement);
    }

    [Test]
    public async Task Should_Return200_With_BuildVersion_CommitHash_On_V1Path()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertVersionMetadataFields(document.RootElement);
    }

    [Test]
    public async Task Should_MatchBuildVersion_ToAssemblyVersion()
    {
        var response = await _client!.GetAsync("/api/version");
        var assemblyVersion = typeof(VersionController).Assembly.GetName().Version?.ToString();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("buildVersion").GetString().ShouldBe(assemblyVersion);
        document.RootElement.GetProperty("assemblyVersion").GetString().ShouldBe(assemblyVersion);
    }

    [Test]
    public async Task Should_ExtractCommitHash_FromInformationalVersion_WhenPresent()
    {
        var assembly = typeof(VersionController).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var expectedCommitHash = VersionMetadataReader.ReadCommitHash(informationalVersion);

        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var commitHashElement = document.RootElement.GetProperty("commitHash");
        if (expectedCommitHash is null)
            commitHashElement.ValueKind.ShouldBe(JsonValueKind.Null);
        else
            commitHashElement.GetString().ShouldBe(expectedCommitHash);
    }

    [Test]
    public async Task Should_PreserveBackwardCompatibility_WithExistingVersionFields()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("assemblyVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("informationalVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("environment").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("machineName").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrEmpty();
    }

    private static void AssertVersionMetadataFields(JsonElement root)
    {
        root.GetProperty("buildVersion").GetString().ShouldNotBeNullOrEmpty();
        root.TryGetProperty("commitHash", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200_When_GetTime_V1Path()
    {
        var response = await _client!.GetAsync("/api/v1.0/time");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
    }

    [Test]
    public async Task Should_IncludeSupportedVersionsHeader_When_GetVersionedEndpoint()
    {
        var response = await _client!.GetAsync("/api/v1.0/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("api-supported-versions", out var values).ShouldBeTrue();
        values.ShouldNotBeNull();
        string.Join(", ", values!).ShouldContain("1.0");
    }
}
