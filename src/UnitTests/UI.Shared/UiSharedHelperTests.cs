using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavRailCssTests
{
    [Test]
    public void ShouldCollapseAppContainer_WhenWideViewportAndNavHidden()
    {
        NavRailCss.AppContainerClass(isNarrowViewport: false, navVisible: false)
            .ShouldBe("modern-app rail-collapsed");
    }

    [Test]
    public void ShouldUseOpenSidebarClass_WhenNarrowViewportAndNavVisible()
    {
        NavRailCss.SidebarClass(isNarrowViewport: true, navVisible: true)
            .ShouldBe("modern-sidebar open");
    }

    [Test]
    public void ShouldHideRailSidebar_WhenWideViewportAndNavHidden()
    {
        NavRailCss.SidebarClass(isNarrowViewport: false, navVisible: false)
            .ShouldBe("modern-sidebar rail-hidden");
    }

    [Test]
    public void ShouldKeepDefaultAppContainer_WhenWideViewportAndNavVisible()
    {
        NavRailCss.AppContainerClass(isNarrowViewport: false, navVisible: true)
            .ShouldBe("modern-app");
    }

    [Test]
    public void ShouldKeepDefaultSidebar_WhenNarrowViewportAndNavHidden()
    {
        NavRailCss.SidebarClass(isNarrowViewport: true, navVisible: false)
            .ShouldBe("modern-sidebar");
    }
}

[TestFixture]
public class BusActivityTaggerTests
{
    [Test]
    public void ShouldTagScalarProperties_WhenMessageHasValues()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("Test");
        using var activity = source.StartActivity("bus");
        activity.ShouldNotBeNull();

        BusActivityTagger.AddScalarPropertyTags(new TestBusMessage { Number = "WO-1" }, activity!);

        activity!.GetTagItem("bus.message.Number").ShouldBe("WO-1");
    }

    private sealed class TestBusMessage
    {
        public string Number { get; init; } = string.Empty;
    }
}
