using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class EchoControllerTests
{
    [Test]
    public void Get_WithMessage_ReturnsMessage()
    {
        var controller = new EchoController();
        var result = controller.Get("hello") as ContentResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content, Is.EqualTo("hello"));
        Assert.That(result.ContentType, Is.EqualTo("text/plain"));
    }

    [Test]
    public void Get_WithoutMessage_ReturnsEmptyString()
    {
        var controller = new EchoController();
        var result = controller.Get(null) as ContentResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content, Is.EqualTo(string.Empty));
        Assert.That(result.ContentType, Is.EqualTo("text/plain"));
    }

    [Test]
    public void Get_WithEmptyMessage_ReturnsEmptyString()
    {
        var controller = new EchoController();
        var result = controller.Get(string.Empty) as ContentResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content, Is.EqualTo(string.Empty));
        Assert.That(result.ContentType, Is.EqualTo("text/plain"));
    }
}
