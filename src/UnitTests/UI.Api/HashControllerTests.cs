using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HashControllerTests
{
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const string AbcMd5 = "900150983cd24fb0d6963f7d28e17f72";
    private const string AbcSha1 = "a9993e364706816aba3e25717850c26c9cd0d89d";

    [Test]
    public void Post_Should_ReturnLowercaseSha256_When_TextIsAbc()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest { Text = "abc" });

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(AbcSha256);
        payload.Md5.ShouldBeNull();
        payload.Sha1.ShouldBeNull();
    }

    [Test]
    public void Post_Should_ReturnEmptyStringSha256_When_TextIsEmpty()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest { Text = string.Empty });

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(EmptySha256);
    }

    [Test]
    public void Post_Should_IncludeMd5AndSha1_When_FlagsTrue()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest
        {
            Text = "abc",
            IncludeMd5 = true,
            IncludeSha1 = true
        });

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(AbcSha256);
        payload.Md5.ShouldBe(AbcMd5);
        payload.Sha1.ShouldBe(AbcSha1);
    }

    [Test]
    public void Post_Should_OmitOptionalHashes_When_FlagsAbsentOrFalse()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest { Text = "abc" });

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("sha256", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("md5", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("sha1", out _).ShouldBeFalse();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextMissingOrNull()
    {
        var controller = CreateController();

        var missing = controller.Post(null);
        var nullText = controller.Post(new HashRequest { Text = null });

        AssertProblemDetails(missing);
        AssertProblemDetails(nullText);
    }

    private static HashController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static void AssertProblemDetails(IActionResult result)
    {
        var objectResult = result.ShouldBeAssignableTo<ObjectResult>();
        objectResult!.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }
}
