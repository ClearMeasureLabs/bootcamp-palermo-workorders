using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoRequestReflectionTests
{
    [Test]
    public void BuildHeaders_Should_IncludeDiagnosticHeaders_When_Present()
    {
        var headers = new HeaderDictionary
        {
            ["Accept"] = "application/json",
            ["User-Agent"] = "EchoTest/1.0",
            ["X-Forwarded-For"] = "203.0.113.1",
            ["Cache-Control"] = "no-cache"
        };

        var result = EchoRequestReflection.BuildHeaders(headers);

        result["Accept"].ShouldBe("application/json");
        result["User-Agent"].ShouldBe("EchoTest/1.0");
        result["X-Forwarded-For"].ShouldBe("203.0.113.1");
        result["Cache-Control"].ShouldBe("no-cache");
    }

    [Test]
    public void BuildHeaders_Should_OmitHopByHopHeaders_When_Present()
    {
        var headers = new HeaderDictionary
        {
            ["Connection"] = "keep-alive",
            ["Keep-Alive"] = "timeout=5",
            ["Proxy-Authorization"] = "Basic abc",
            ["Transfer-Encoding"] = "chunked",
            ["User-Agent"] = "EchoTest/1.0"
        };

        var result = EchoRequestReflection.BuildHeaders(headers);

        result.ContainsKey("Connection").ShouldBeFalse();
        result.ContainsKey("Keep-Alive").ShouldBeFalse();
        result.ContainsKey("Proxy-Authorization").ShouldBeFalse();
        result.ContainsKey("Transfer-Encoding").ShouldBeFalse();
        result["User-Agent"].ShouldBe("EchoTest/1.0");
    }

    [Test]
    public void BuildHeaders_Should_RedactSensitiveHeaders_When_Present()
    {
        var headers = new HeaderDictionary
        {
            ["Authorization"] = "Bearer secret",
            ["Cookie"] = "session=abc",
            ["Set-Cookie"] = "session=abc",
            ["X-Api-Key"] = "my-secret-key",
            ["User-Agent"] = "EchoTest/1.0"
        };

        var result = EchoRequestReflection.BuildHeaders(headers);

        result.ContainsKey("Authorization").ShouldBeFalse();
        result.ContainsKey("Cookie").ShouldBeFalse();
        result.ContainsKey("Set-Cookie").ShouldBeFalse();
        result["X-Api-Key"].ShouldBe("[present]");
        result["User-Agent"].ShouldBe("EchoTest/1.0");
    }

    [Test]
    public void BuildHeaders_Should_ReturnEmptyDictionary_When_NoIncludedHeaders()
    {
        var headers = new HeaderDictionary
        {
            ["X-Custom-Header"] = "value"
        };

        var result = EchoRequestReflection.BuildHeaders(headers);

        result.Count.ShouldBe(0);
    }

    [Test]
    public void BuildQuery_Should_ReturnFirstValuePerKey_When_QueryPresent()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["foo"] = "bar",
            ["baz"] = "1"
        });

        var result = EchoRequestReflection.BuildQuery(query);

        result["foo"].ShouldBe("bar");
        result["baz"].ShouldBe("1");
    }
}
