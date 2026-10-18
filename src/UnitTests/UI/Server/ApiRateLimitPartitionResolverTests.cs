using System.Security.Claims;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiRateLimitPartitionResolverTests
{
    [Test]
    public void Resolve_ShouldUseSamePartition_ForUserNamesDifferingOnlyByCase()
    {
        var alicePartition = ResolveForUser("Alice");
        var aliceLowerPartition = ResolveForUser("alice");

        alicePartition.ShouldBe(aliceLowerPartition);
        alicePartition.ShouldBe("user:alice");
    }

    private static string ResolveForUser(string userName)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, userName)],
                authenticationType: "Test"))
        };

        return ApiRateLimitPartitionResolver.Resolve(httpContext, apiKeyHeaderName: "X-Api-Key");
    }
}
