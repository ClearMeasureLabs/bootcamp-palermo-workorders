using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Lamar;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WhatDoIHaveControllerTests
{
    [Test]
    public void Services_Should_ReturnContainerWhatDoIHaveText()
    {
        using var container = new Container(_ => { });
        var controller = new WhatDoIHaveController();

        var result = controller.Services(container);

        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Lamar");
    }

    [Test]
    public void Scanning_Should_ReturnContainerWhatDidIScanText()
    {
        using var container = new Container(_ => { });
        var controller = new WhatDoIHaveController();

        var result = controller.Scanning(container);

        result.ShouldNotBeNullOrEmpty();
    }
}
