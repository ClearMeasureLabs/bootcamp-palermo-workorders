using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    [Test]
    public void GetRandom_Should_ReturnJsonWithStubValues_When_TypeIsSupported()
    {
        var stub = new StubRandomToolPayloadGenerator();
        var controller = new ToolsRandomController(stub)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var number = controller.GetRandom("number").ShouldBeOfType<ContentResult>();
        number.StatusCode.ShouldBe(200);
        var numberJson = number.Content.ShouldNotBeNull();
        numberJson.ShouldContain("\"type\":\"number\"");
        numberJson.ShouldContain("\"value\":42");

        var str = controller.GetRandom("STRING").ShouldBeOfType<ContentResult>();
        var strJson = str.Content.ShouldNotBeNull();
        strJson.ShouldContain("\"type\":\"string\"");
        strJson.ShouldContain("\"value\":\"aaaaaaaaaaaaaaaaaaaaaaaa\"");

        var uuid = controller.GetRandom("uuid").ShouldBeOfType<ContentResult>();
        var uuidJson = uuid.Content.ShouldNotBeNull();
        uuidJson.ShouldContain("\"type\":\"uuid\"");
        uuidJson.ShouldContain("11111111-1111-1111-1111-111111111111");

        var color = controller.GetRandom("color").ShouldBeOfType<ContentResult>();
        var colorJson = color.Content.ShouldNotBeNull();
        colorJson.ShouldContain("\"type\":\"color\"");
        colorJson.ShouldContain("#aabbcc");
    }

    [Test]
    public void GetRandom_Should_Return400_When_TypeMissingOrInvalid()
    {
        var stub = new StubRandomToolPayloadGenerator();
        var controller = new ToolsRandomController(stub)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var missing = controller.GetRandom(null).ShouldBeOfType<ObjectResult>();
        missing.StatusCode.ShouldBe(400);

        var whitespace = controller.GetRandom("   ").ShouldBeOfType<ObjectResult>();
        whitespace.StatusCode.ShouldBe(400);

        var bad = controller.GetRandom("nope").ShouldBeOfType<ObjectResult>();
        bad.StatusCode.ShouldBe(400);
    }

    private sealed class StubRandomToolPayloadGenerator : IRandomToolPayloadGenerator
    {
        public int NextInt32() => 42;

        public string NextAlphanumericString(int length) => new string('a', length);

        public Guid NextGuid() => Guid.Parse("11111111-1111-1111-1111-111111111111");

        public string NextCssHexColor() => "#aabbcc";
    }
}
