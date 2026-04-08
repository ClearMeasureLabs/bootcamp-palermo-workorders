using System.IO.Compression;
using System.Net;
using System.Text;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestDecompressionWebTests
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
    public async Task Should_ReturnOriginalBody_When_PostWithoutContentEncoding()
    {
        const string payload = "uncompressed-body";

        var response = await _client!.PostAsync(
            "/__test/request-body-echo",
            new StringContent(payload, Encoding.UTF8, "text/plain"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe(payload);
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_GzipContentEncoding_ButBodyIsNotValidGzip()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/__test/request-body-echo")
        {
            Content = new ByteArrayContent([0x01, 0x02, 0x03, 0xff])
        };
        request.Content.Headers.ContentEncoding.Add("gzip");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_DeflateContentEncoding_ButBodyIsNotValidZlib()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/__test/request-body-echo")
        {
            Content = new ByteArrayContent([0x01, 0x02, 0x03, 0xff])
        };
        request.Content.Headers.ContentEncoding.Add("deflate");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ReturnDecompressedBody_When_PostGzipEncodedRequest()
    {
        const string payload = "{\"hello\":\"world\"}";
        await using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(payload));
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/__test/request-body-echo")
        {
            Content = new ByteArrayContent(compressed.ToArray())
        };
        request.Content.Headers.ContentEncoding.Add("gzip");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe(payload);
    }

    [Test]
    public async Task Should_ReturnDecompressedBody_When_PostDeflateEncodedRequest()
    {
        const string payload = "plain-text-payload";
        await using var compressed = new MemoryStream();
        await using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            await zlib.WriteAsync(Encoding.UTF8.GetBytes(payload));
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/__test/request-body-echo")
        {
            Content = new ByteArrayContent(compressed.ToArray())
        };
        request.Content.Headers.ContentEncoding.Add("deflate");

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe(payload);
    }
}
