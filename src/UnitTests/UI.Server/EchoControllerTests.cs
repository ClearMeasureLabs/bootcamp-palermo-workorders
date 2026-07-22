using ClearMeasure.Bootcamp.UI.Server.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class EchoControllerTests
{
    [Fact]
    public void Get_WithMessage_ReturnsMessage()
    {
        var controller = new EchoController();
        var result = controller.Get("hello") as ContentResult;

        result.Should().NotBeNull();
        result!.Content.Should().Be("hello");
        result.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void Get_WithoutMessage_ReturnsEmptyString()
    {
        var controller = new EchoController();
        var result = controller.Get(null) as ContentResult;

        result.Should().NotBeNull();
        result!.Content.Should().Be(string.Empty);
        result.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void Get_WithEmptyMessage_ReturnsEmptyString()
    {
        var controller = new EchoController();
        var result = controller.Get(string.Empty) as ContentResult;

        result.Should().NotBeNull();
        result!.Content.Should().Be(string.Empty);
        result.ContentType.Should().Be("text/plain");
    }
}
