using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class RateLimitingPartitionKeyTests
{
    [Test]
    public void Should_UseApiKeyPartition_When_HeaderPresent()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-API-Key"] = "secret-1";
        ApiRateLimitingExtensions.ResolvePartitionKey(ctx, "X-API-Key").ShouldBe("key:secret-1");
    }

    [Test]
    public void Should_UseIpPartition_When_NoApiKey()
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
        ApiRateLimitingExtensions.ResolvePartitionKey(ctx, "X-API-Key").ShouldBe("ip:10.0.0.5");
    }

    [Test]
    public void Should_UseAnonymousPartition_When_NoIpAndNoKey()
    {
        var ctx = new DefaultHttpContext();
        ApiRateLimitingExtensions.ResolvePartitionKey(ctx, "X-API-Key").ShouldBe("anonymous");
    }
}
