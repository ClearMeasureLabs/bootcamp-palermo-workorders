using System.Net;
using System.Net.Http.Json;
using System.Text;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ToolsHashEndpointWebTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;

    [SetUp]
    public void SetUp() => _factory = new ApiVersioningRoutingWebApplicationFactory();

    [TearDown]
    public async Task TearDown()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        _factory = null;
    }

    [TestCase("/api/tools/hash")]
    [TestCase("/api/v1.0/tools/hash")]
    public async Task Should_Return200AndGoldenDigests_When_PostHello(string route)
    {
        using var client = _factory!.CreateClient();

        var response = await client.PostAsJsonAsync(route, new HashTextRequest { Text = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var body = await response.Content.ReadFromJsonAsync<HashTextResponse>();
        body.ShouldNotBeNull();
        body!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        body.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        body.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public async Task Should_ReturnSameDigests_OnUnversionedAndV1Routes()
    {
        using var client = _factory!.CreateClient();

        var legacy = await client.PostAsJsonAsync("/api/tools/hash", new HashTextRequest { Text = "hello" });
        var v1 = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new HashTextRequest { Text = "hello" });

        var a = await legacy.Content.ReadFromJsonAsync<HashTextResponse>();
        var b = await v1.Content.ReadFromJsonAsync<HashTextResponse>();
        a.ShouldNotBeNull();
        b.ShouldNotBeNull();
        a!.Sha256.ShouldBe(b!.Sha256);
        a.Md5.ShouldBe(b.Md5);
        a.Sha1.ShouldBe(b.Sha1);
    }

    [TestCase("/api/tools/hash")]
    [TestCase("/api/v1.0/tools/hash")]
    public async Task Should_Return200AndStableUnicodeDigests_When_PostUtf8Text(string route)
    {
        using var client = _factory!.CreateClient();
        var text = "日本";

        var response = await client.PostAsJsonAsync(route, new HashTextRequest { Text = text });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HashTextResponse>();
        body.ShouldNotBeNull();
        body!.Sha256.ShouldBe("cf2abf0c5be326cb922a70f8163f91079c4d9aa8655c60ead89ad545c9de2e92");
        body.Md5.ShouldBe("4dbed2e657457884e67137d3514119b3");
        body.Sha1.ShouldBe("44da6bbcf285cdb317aa91174217990d1b94d64d");
    }

    [TestCase("/api/tools/hash")]
    [TestCase("/api/v1.0/tools/hash")]
    public async Task Should_Return200AndEmptyStringDigests_When_TextEmpty(string route)
    {
        using var client = _factory!.CreateClient();

        var response = await client.PostAsJsonAsync(route, new HashTextRequest { Text = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HashTextResponse>();
        body.ShouldNotBeNull();
        body!.Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        body.Md5.ShouldBe("d41d8cd98f00b204e9800998ecf8427e");
        body.Sha1.ShouldBe("da39a3ee5e6b4b0d3255bfef95601890afd80709");
    }

    [TestCase("/api/tools/hash")]
    [TestCase("/api/v1.0/tools/hash")]
    public async Task Should_ReturnNotSuccess_When_JsonMalformed(string route)
    {
        using var client = _factory!.CreateClient();
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(route, content);

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestCase("/api/tools/hash")]
    [TestCase("/api/v1.0/tools/hash")]
    public async Task Should_ReturnNotSuccess_When_TextPropertyMissing(string route)
    {
        using var client = _factory!.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(route, content);

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ReturnNotSuccess_When_UnsupportedVersion()
    {
        using var client = _factory!.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v2.0/tools/hash", new HashTextRequest { Text = "x" });

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
