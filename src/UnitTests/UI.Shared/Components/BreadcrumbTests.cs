using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class BreadcrumbTests
{
    [Test]
    public void Breadcrumb_RendersAllItems_InOrder()
    {
        using var ctx = CreateContext();
        var items = new List<BreadcrumbItem>
        {
            new("Home", "/", false),
            new("Work Orders", "/workorder/search", false),
            new("Search", null, true)
        };

        var component = ctx.RenderComponent<Breadcrumb>(p => p.Add(x => x.Items, items));

        component.Markup.ShouldContain("Home");
        component.Markup.ShouldContain("Work Orders");
        component.Markup.ShouldContain("Search");

        var homeIndex = component.Markup.IndexOf("Home", StringComparison.Ordinal);
        var workOrdersIndex = component.Markup.IndexOf("Work Orders", StringComparison.Ordinal);
        var searchIndex = component.Markup.IndexOf("Search", StringComparison.Ordinal);
        homeIndex.ShouldBeLessThan(workOrdersIndex);
        workOrdersIndex.ShouldBeLessThan(searchIndex);
    }

    [Test]
    public void Breadcrumb_LastItem_HasAriaCurrentPage_NotClickable()
    {
        using var ctx = CreateContext();
        var items = new List<BreadcrumbItem>
        {
            new("Home", "/", false),
            new("Counter", null, true)
        };

        var component = ctx.RenderComponent<Breadcrumb>(p => p.Add(x => x.Items, items));

        var active = component.Find(".breadcrumb-active");
        active.GetAttribute("aria-current").ShouldBe("page");
        active.TextContent.ShouldBe("Counter");
        component.FindAll("a").Count.ShouldBe(1);
    }

    [Test]
    public void Breadcrumb_ParentItems_AreClickable()
    {
        using var ctx = CreateContext();
        var items = new List<BreadcrumbItem>
        {
            new("Home", "/", false),
            new("Work Orders", "/workorder/search", false),
            new("Search", null, true)
        };

        var component = ctx.RenderComponent<Breadcrumb>(p => p.Add(x => x.Items, items));
        var links = component.FindAll("a.breadcrumb-link");

        links.Count.ShouldBe(2);
        links[0].GetAttribute("href").ShouldBe("/");
        links[1].GetAttribute("href").ShouldBe("/workorder/search");
    }

    [Test]
    public void Breadcrumb_SeparatorsShowBetweenItems()
    {
        using var ctx = CreateContext();
        var items = new List<BreadcrumbItem>
        {
            new("Home", "/", false),
            new("Work Orders", "/workorder/search", false),
            new("Search", null, true)
        };

        var component = ctx.RenderComponent<Breadcrumb>(p => p.Add(x => x.Items, items));
        component.FindAll(".breadcrumb-separator").Count.ShouldBe(2);
    }

    [Test]
    public void Breadcrumb_NavElement_HasBreadcrumbAriaLabel()
    {
        using var ctx = CreateContext();
        var items = new List<BreadcrumbItem> { new("Home", "/", false), new("Counter", null, true) };

        var component = ctx.RenderComponent<Breadcrumb>(p => p.Add(x => x.Items, items));
        var nav = component.Find($"[data-testid='{nameof(Breadcrumb.Elements.Breadcrumb)}']");

        nav.TagName.ShouldBe("NAV");
        nav.GetAttribute("aria-label").ShouldBe("breadcrumb");
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }
}
