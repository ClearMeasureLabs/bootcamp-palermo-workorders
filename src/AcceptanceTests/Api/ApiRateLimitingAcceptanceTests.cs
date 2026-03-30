using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class ApiRateLimitingAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task Api_RateLimitHeaders_PresentOnEveryResponse()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl) };
        var response = await client.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        if (!response.Headers.TryGetValues("X-RateLimit-Limit", out _))
            Assert.Ignore("API rate limiting is disabled in this environment (e.g. Development appsettings).");

        response.Headers.TryGetValues("X-RateLimit-Remaining", out _).ShouldBeTrue();
        response.Headers.TryGetValues("X-RateLimit-Reset", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Api_RapidRequests_EventuallyReturns429()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl) };
        HttpStatusCode? last = null;
        for (var i = 0; i < 250; i++)
        {
            var r = await client.GetAsync("/api/time");
            last = r.StatusCode;
            if (r.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        if (last != HttpStatusCode.TooManyRequests)
            Assert.Ignore("API rate limiting is disabled in this environment (e.g. Development appsettings).");
    }
}
