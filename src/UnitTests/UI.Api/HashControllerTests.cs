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
    private static HashController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static HashTextResponse Deserialize(ContentResult content) =>
        JsonSerializer.Deserialize<HashTextResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions)!;

    [Test]
    public void Sha256_Should_ReturnValidHexString_When_InputIsText()
    {
        var result = CreateController().Post(new HashTextRequest("hello"));

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content);
        payload.Sha256.Length.ShouldBe(64);
        payload.Sha256.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Test]
    public void Md5_Should_ReturnValidHexString_When_Enabled()
    {
        var result = CreateController().Post(new HashTextRequest("hello"));

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content);
        payload.Md5.Length.ShouldBe(32);
        payload.Md5.ShouldMatch("^[0-9a-f]{32}$");
    }

    [Test]
    public void Sha1_Should_ReturnValidHexString_When_Enabled()
    {
        var result = CreateController().Post(new HashTextRequest("hello"));

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content);
        payload.Sha1.Length.ShouldBe(40);
        payload.Sha1.ShouldMatch("^[0-9a-f]{40}$");
    }

    [Test]
    public void Hashes_Should_Match_KnownVectors_When_InputIsStandard()
    {
        var emptyResult = CreateController().Post(new HashTextRequest(""));
        var empty = Deserialize(emptyResult.ShouldBeOfType<ContentResult>());
        empty.Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        empty.Md5.ShouldBe("d41d8cd98f00b204e9800998ecf8427e");
        empty.Sha1.ShouldBe("da39a3ee5e6b4b0d3255bfef95601890afd80709");

        var helloResult = CreateController().Post(new HashTextRequest("hello"));
        var hello = Deserialize(helloResult.ShouldBeOfType<ContentResult>());
        hello.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        hello.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        hello.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public void Should_Return400_When_TextIsNull()
    {
        var result = CreateController().Post(new HashTextRequest(null));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_Return400_When_TextIsMissing()
    {
        var result = CreateController().Post(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_Return400_When_TextIsWhitespace()
    {
        var result = CreateController().Post(new HashTextRequest("   "));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Should_Return200_When_TextIsEmpty()
    {
        var result = CreateController().Post(new HashTextRequest(""));

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        Deserialize(content).Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Test]
    public void Should_Return200_When_TextIsUtf8()
    {
        var result = CreateController().Post(new HashTextRequest("café"));

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        Deserialize(content).Sha256.ShouldBe("850f7dc43910ff890f8879c0ed26fe697c93a067ad93a7d50f466a7028a9bf4e");
    }
}
