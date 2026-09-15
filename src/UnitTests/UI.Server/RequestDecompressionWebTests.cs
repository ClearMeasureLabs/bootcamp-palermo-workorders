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
