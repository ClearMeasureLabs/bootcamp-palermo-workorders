using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HashControllerTests
{
    private static readonly HashController Controller = new()
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    [Test]
    public void Should_ReturnSha256_When_TextProvided()
    {
        var result = Controller.Post(new HashRequest("sample"));

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = Deserialize(content.Content!);
        payload!.Sha256.ShouldNotBeNullOrEmpty();
        payload.Sha256.Length.ShouldBe(64);
    }

    [Test]
    public void Should_ReturnAllHashes_When_TextProvided()
    {
        var result = Controller.Post(new HashRequest("sample"));

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content.Content!);
        payload!.Sha256.ShouldNotBeNullOrEmpty();
        payload.Md5.ShouldNotBeNullOrEmpty();
        payload.Sha1.ShouldNotBeNullOrEmpty();
        payload.Md5.Length.ShouldBe(32);
        payload.Sha1.Length.ShouldBe(40);
    }

    [Test]
    public void Should_ReturnSha256ForKnownVector_When_TextIsHello()
    {
        var result = Controller.Post(new HashRequest("hello"));

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content.Content!);
        payload!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        payload.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public void Should_ReturnBadRequest_When_TextMissing()
    {
        var result = Controller.Post(null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_ReturnBadRequest_When_TextNull()
    {
        var result = Controller.Post(new HashRequest(null!));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_ReturnBadRequest_When_TextWhitespaceOnly()
    {
        var result = Controller.Post(new HashRequest("   "));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_EncodeUtf8_When_TextContainsUnicode()
    {
        const string text = "café ☕";
        var result = Controller.Post(new HashRequest(text));

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content.Content!);
        payload!.Sha256.ShouldBe(ComputeExpectedSha256(text));
    }

    private static string ComputeExpectedSha256(string text)
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(text);
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(utf8));
    }

    private static HashResponse? Deserialize(string json) =>
        JsonSerializer.Deserialize<HashResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
}
