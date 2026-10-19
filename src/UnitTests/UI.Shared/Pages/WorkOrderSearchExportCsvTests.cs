using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSearchExportCsvTests
{
    [Test]
    public async Task ExportCsvLink_Should_IncludeCurrentFiltersInUrl()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<WorkOrderSearch>();

        var link = cut.Find($"[data-testid='{WorkOrderSearch.Elements.ExportCsvLink}']");
        link.GetAttribute("href").ShouldBe("/api/workorders/export");

        cut.Instance.Model.Filters.Status = "Draft";
        cut.Instance.Model.Filters.Creator = "alice";
        cut.Instance.Model.Filters.Assignee = "bob";

        var href = cut.Instance.ExportCsvUrl;
        href.ShouldContain("/api/workorders/export");
        href.ShouldContain("status=Draft");
        href.ShouldContain("creator=alice");
        href.ShouldContain("assignee=bob");
    }

    private static BunitContext CreateContext(string loggedInAs = "jpalermo")
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(new WorkOrderSearchState());
        var store = new StubUserSessionStore { Username = loggedInAs };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login(loggedInAs).GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
        return ctx;
    }
}
