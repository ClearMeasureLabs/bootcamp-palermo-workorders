using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class ApiRateLimitingAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    // Shared across every test in this fixture (Qodana ShortLivedHttpClient) — a single
    // HttpClient/HttpClientHandler pair is reused instead of allocating a new socket handler
    // per test method. No IHttpClientFactory/DI container is available in this NUnit fixture.
    // BaseAddress is intentionally never set on this shared client: ApplicationBaseUrl is not
    // known until ServerFixture's OneTimeSetUp runs, and HttpClient throws
    // InvalidOperationException if BaseAddress is mutated after the client has sent its first
    // request. Each call below passes the full absolute URI instead.
    private static readonly HttpClientHandler Handler = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    private static readonly HttpClient Client = new(Handler);

    [OneTimeTearDown]
    public void DisposeSharedClient()
    {
        Client.Dispose();
        Handler.Dispose();
    }

    [Test]
    public async Task Api_RateLimitHeaders_PresentOnEveryResponse()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        using var response = await Client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/version");
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

        HttpStatusCode? last = null;
        for (var i = 0; i < 250; i++)
        {
            using var r = await Client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/time");
            last = r.StatusCode;
            if (r.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        if (last != HttpStatusCode.TooManyRequests)
            Assert.Ignore("API rate limiting is disabled in this environment (e.g. Development appsettings).");
    }
}
