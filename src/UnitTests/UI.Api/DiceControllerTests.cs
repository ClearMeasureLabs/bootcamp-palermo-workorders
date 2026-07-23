using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DiceControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextInteger_When_SeededRandom()
    {
        var controller = new DiceController(new Random(42))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        int.TryParse(content.Content, out var value).ShouldBeTrue();
        value.ShouldBeInRange(1, 6);
    }

    [TestCase(1)]
    [TestCase(42)]
    [TestCase(100)]
    [TestCase(999)]
    public void Get_Should_ReturnValueInRange1Through6_When_SeededRandom(int seed)
    {
        var controller = new DiceController(new Random(seed))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        int.TryParse(content.Content, out var value).ShouldBeTrue();
        value.ShouldBeInRange(1, 6);
    }
}
