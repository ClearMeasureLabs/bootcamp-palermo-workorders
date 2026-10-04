using System.Net;
using System.Text;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class IdempotencyMiddlewareWebTests
{
    [Test]
    public async Task Should_Replay200_When_SamePostBodyAndIdempotencyKey()
    {
        await using var factory = new WebServiceMessageValidationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/_test/idempotency-probe");
        req1.Headers.Add(IdempotencyConstants.HeaderName, "key-a");
        req1.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var r1 = await client.SendAsync(req1);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r1.Content.ReadAsStringAsync()).ShouldBe("count=1");

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/_test/idempotency-probe");
        req2.Headers.Add(IdempotencyConstants.HeaderName, "key-a");
        req2.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var r2 = await client.SendAsync(req2);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r2.Content.ReadAsStringAsync()).ShouldBe("count=1");
    }

    [Test]
    public async Task Should_Replay200_When_SamePutBodyAndIdempotencyKey()
    {
        await using var factory = new WebServiceMessageValidationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var req1 = new HttpRequestMessage(HttpMethod.Put, "/api/_test/idempotency-probe");
        req1.Headers.Add(IdempotencyConstants.HeaderName, "key-put");
        req1.Content = new StringContent("x", Encoding.UTF8, "text/plain");

        var r1 = await client.SendAsync(req1);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r1.Content.ReadAsStringAsync()).ShouldBe("count=1");

        using var req2 = new HttpRequestMessage(HttpMethod.Put, "/api/_test/idempotency-probe");
        req2.Headers.Add(IdempotencyConstants.HeaderName, "key-put");
        req2.Content = new StringContent("x", Encoding.UTF8, "text/plain");

        var r2 = await client.SendAsync(req2);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r2.Content.ReadAsStringAsync()).ShouldBe("count=1");
    }

    [Test]
    public async Task Should_Return409_When_SameKeyDifferentBody()
    {
        await using var factory = new WebServiceMessageValidationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/_test/idempotency-probe");
        req1.Headers.Add(IdempotencyConstants.HeaderName, "shared-key");
        req1.Content = new StringContent("a", Encoding.UTF8, "text/plain");
        (await client.SendAsync(req1)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/_test/idempotency-probe");
        req2.Headers.Add(IdempotencyConstants.HeaderName, "shared-key");
        req2.Content = new StringContent("b", Encoding.UTF8, "text/plain");
        var r2 = await client.SendAsync(req2);
        r2.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Should_Return400_When_IdempotencyKeyTooLong()
    {
        await using var factory = new IdempotencyMaxKeyWebApplicationFactory();
        using var client = factory.CreateClient();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/_test/idempotency-probe");
        req.Headers.Add(IdempotencyConstants.HeaderName, new string('x', 20));
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var r = await client.SendAsync(req);
        r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}

/// <summary>
/// Host with <see cref="IdempotencyOptions.MaxKeyLength"/> set to 10 for validation testing.
/// </summary>
internal sealed class IdempotencyMaxKeyWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["Idempotency:MaxKeyLength"] = "10"
            });
        });
    }
}
