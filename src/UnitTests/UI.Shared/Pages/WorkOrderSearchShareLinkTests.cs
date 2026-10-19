using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSearchShareLinkTests
{
    private const string CurrentUsername = "jpalermo";

    private static BunitContext CreateContext(IBus? bus = null, string loggedInAs = CurrentUsername)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(bus ?? new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(new WorkOrderSearchState());

        var store = new StubUserSessionStore { Username = loggedInAs };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login(loggedInAs).GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        return ctx;
    }

    [Test]
    public async Task ShareSearchLink_WhenNoFilters_OmitsQueryString()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var link = component.Find($"[data-testid='{WorkOrderSearch.Elements.ShareThisSearch}'] a");
        var href = link.GetAttribute("href")!;
        href.ShouldContain("/workorder/search");
        href.ShouldNotContain("?");
        link.TextContent.ShouldBe(href);
    }

    [Test]
    public async Task ShareSearchLink_WhenOneFilter_IncludesOnlyThatParameter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");
        await statusSelect.ChangeAsync(new ChangeEventArgs { Value = "InProgress" });

        var link = component.Find($"[data-testid='{WorkOrderSearch.Elements.ShareThisSearch}'] a");
        var href = link.GetAttribute("href")!;
        href.ShouldContain("/workorder/search?");
        href.ShouldContain("Status=InProgress");
        href.ShouldNotContain("Creator=");
        href.ShouldNotContain("Assignee=");
        href.ShouldNotContain("OverdueOnly=");
    }

    [Test]
    public async Task ShareSearchLink_WhenSeveralFilters_IncludesAllActiveParameters()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();
        await component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}")
            .ChangeAsync(new ChangeEventArgs { Value = "jpalermo" });
        await component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}")
            .ChangeAsync(new ChangeEventArgs { Value = "bob" });
        await component.Find($"#{WorkOrderSearch.Elements.StatusSelect}")
            .ChangeAsync(new ChangeEventArgs { Value = "Draft" });
        await component.Find($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}")
            .ChangeAsync(new ChangeEventArgs { Value = true });

        var link = component.Find($"[data-testid='{WorkOrderSearch.Elements.ShareThisSearch}'] a");
        var href = link.GetAttribute("href")!;
        href.ShouldContain("Creator=jpalermo");
        href.ShouldContain("Assignee=bob");
        href.ShouldContain("Status=Draft");
        href.ShouldContain("OverdueOnly=True");
    }

    [Test]
    public async Task ShareSearchLink_WhenFilterChanged_UpdatesUrl()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");
        await statusSelect.ChangeAsync(new ChangeEventArgs { Value = "Complete" });

        var link = component.Find($"[data-testid='{WorkOrderSearch.Elements.ShareThisSearch}'] a");
        link.GetAttribute("href")!.ShouldContain("Status=Complete");
    }
}
