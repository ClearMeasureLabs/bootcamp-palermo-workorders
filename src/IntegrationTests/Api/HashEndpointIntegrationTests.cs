using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HashEndpointIntegrationTests
{
    private const string HelloSha256 = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";
    private const string HelloMd5 = "5d41402abc4badb7605b357e99571da9";
    private const string HelloSha1 = "aaf4c61ddcc5e8a2dabede0f4b3ac12fa6cebc15";

    private const string WhitespaceSha256 = "0aad7da77d2ed59c396c99a74e49f3a4524dcdbcb5163251b1433d640247aeb4";
    private const string WhitespaceMd5 = "628631f07321b22d8c176c200c855e1b";
    private const string WhitespaceSha1 = "088fb1a4ab057f4fcf7d487006499060c7fe5773";

    private const string UnicodeSha256 = "cbbcee01a3fc5f1c0db23e02be25316adf28ede876031fdbabe5f4fabe47ed7f";
    private const string UnicodeMd5 = "a4115cc10566f0181d01df50100b37ff";
    private const string UnicodeSha1 = "3d100d877e936e4baff8c55a424233d2383c315f";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
    public async Task Should_Return200AndJsonHashes_When_PostUnversioned()
    {
        var response = await PostJsonAsync(_client!, "/api/tools/hash", """{"text":"hello"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHelloHashesAsync(response);
    }

    [Test]
    public async Task Should_Return200AndJsonHashes_When_PostVersioned()
    {
        var response = await PostJsonAsync(_client!, "/api/v1.0/tools/hash", """{"text":"hello"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHelloHashesAsync(response);
    }

    [Test]
    public async Task Should_Return400_When_TextIsEmptyString()
    {
        var response = await PostJsonAsync(_client!, "/api/tools/hash", """{"text":""}""");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertProblemDetailsAsync(response);
    }

    [Test]
    public async Task Should_Return400_When_TextPropertyMissing()
    {
        var response = await PostJsonAsync(_client!, "/api/tools/hash", "{}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertProblemDetailsAsync(response);
    }

    [Test]
    public async Task Should_Return400_When_RequestBodyIsNull()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tools/hash")
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertProblemDetailsAsync(response);
    }

    [Test]
    public async Task Should_Return200_When_TextIsWhitespaceOnly()
    {
        var response = await PostJsonAsync(_client!, "/api/tools/hash", """{"text":"   "}""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HashTextResponse>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe(WhitespaceSha256);
        payload.Md5.ShouldBe(WhitespaceMd5);
        payload.Sha1.ShouldBe(WhitespaceSha1);
    }

    [Test]
    public async Task Should_Return200_When_TextContainsUnicode()
    {
        var response = await PostJsonAsync(_client!, "/api/tools/hash", """{"text":"héllo 🌍"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HashTextResponse>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe(UnicodeSha256);
        payload.Md5.ShouldBe(UnicodeMd5);
        payload.Sha1.ShouldBe(UnicodeSha1);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndHashProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await PostJsonAsync(client, "/api/tools/hash", """{"text":"hello"}""");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var versioned = await PostJsonAsync(client, "/api/v1.0/tools/hash", """{"text":"hello"}""");
        versioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_ValidApiKeyProvided()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await PostJsonAsync(client, "/api/tools/hash", """{"text":"hello"}""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHelloHashesAsync(response);
    }

    [Test]
    public async Task Should_ReplayIdenticalResponse_When_SameIdempotencyKeyAndBody()
    {
        const string idempotencyKey = "hash-hello-key";
        const string body = """{"text":"hello"}""";

        using var req1 = CreateHashPost("/api/tools/hash", body, idempotencyKey);
        var r1 = await _client!.SendAsync(req1);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body1 = await r1.Content.ReadAsStringAsync();

        using var req2 = CreateHashPost("/api/tools/hash", body, idempotencyKey);
        var r2 = await _client.SendAsync(req2);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body2 = await r2.Content.ReadAsStringAsync();

        body2.ShouldBe(body1);
    }

    private static HttpRequestMessage CreateHashPost(string path, string jsonBody, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add(IdempotencyConstants.HeaderName, idempotencyKey);
        return request;
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string jsonBody)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return await client.PostAsync(path, content);
    }

    private static async Task AssertHelloHashesAsync(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<HashTextResponse>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Sha256.ShouldBe(HelloSha256);
        payload.Md5.ShouldBe(HelloMd5);
        payload.Sha1.ShouldBe(HelloSha1);
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("status", out var status).ShouldBeTrue();
        status.GetInt32().ShouldBe(400);
        doc.RootElement.TryGetProperty("detail", out var detail).ShouldBeTrue();
        detail.GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
