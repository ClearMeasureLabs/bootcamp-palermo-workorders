using System.Net;
using System.Net.Http.Headers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiResponseCompressionWebTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ApiVersioningRoutingWebApplicationFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory?.Dispose();

    [Test]
    public async Task Should_ReturnContentEncodingBrotli_When_ClientAcceptsBrotli()
    {
        using var client = _factory!.CreateDefaultClient(new DecompressionHandler());
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        using var response = await client.GetAsync("/_test/compression-probe");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.ShouldContain("br");
    }

    [Test]
    public async Task Should_ReturnContentEncodingGzip_When_ClientAcceptsGzipOnly()
    {
        using var client = _factory!.CreateDefaultClient(new DecompressionHandler());
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        using var response = await client.GetAsync("/_test/compression-probe");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.ShouldContain("gzip");
    }

    /// <summary>
    /// Prevents <see cref="HttpClient"/> from stripping <c>Content-Encoding</c> so tests can assert compression headers.
    /// </summary>
    private sealed class DecompressionHandler : DelegatingHandler
    {
        public DecompressionHandler() : base(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None })
        {
        }
    }
}
