using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class ConditionalGetEtagTests
{
    [Test]
    public void Should_ReturnTrue_When_IfNoneMatchMatchesWeakEtag()
    {
        var payload = new StubPayload { Id = 1, Name = "a" };
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        var request = new DefaultHttpContext().Request;
        request.Headers.IfNoneMatch = etag.ToString();
        ConditionalGetEtag.IfNoneMatchIncludesEtag(request, etag).ShouldBeTrue();
    }

    [Test]
    public void Should_ReturnFalse_When_IfNoneMatchDoesNotMatch()
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(new StubPayload { Id = 1 });
        var request = new DefaultHttpContext().Request;
        request.Headers.IfNoneMatch = "W/\"other\"";
        ConditionalGetEtag.IfNoneMatchIncludesEtag(request, etag).ShouldBeFalse();
    }

    [Test]
    public void Should_ReturnTrue_When_IfNoneMatchIsWildcard()
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(new StubPayload { Id = 1 });
        var request = new DefaultHttpContext().Request;
        request.Headers.IfNoneMatch = "*";
        ConditionalGetEtag.IfNoneMatchIncludesEtag(request, etag).ShouldBeTrue();
    }

    [Test]
    public void Should_ProduceSameEtag_When_JsonPayloadEquivalent()
    {
        var a = ConditionalGetEtag.CreateWeakEtagForJson(new StubPayload { Id = 2, Name = "x" });
        var b = ConditionalGetEtag.CreateWeakEtagForJson(new StubPayload { Id = 2, Name = "x" });
        a.ToString().ShouldBe(b.ToString());
    }

    private sealed record StubPayload
    {
        public int Id { get; init; }
        public string? Name { get; init; }
    }
}
