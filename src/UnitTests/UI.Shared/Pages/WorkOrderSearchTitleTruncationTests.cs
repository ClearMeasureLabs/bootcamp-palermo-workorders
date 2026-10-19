using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSearchTitleTruncationTests
{
    [Test]
    public async Task ShouldTruncateLongTitleAndSetTooltip()
    {
        var fullTitle = new string('A', 80);
        var workOrder = new WorkOrder
        {
            Number = "WO-LNG",
            Title = fullTitle
        };
        await using var ctx = CreateContext(new StubBus([workOrder]));
        var component = ctx.Render<WorkOrderSearch>();
        await component.WaitForAssertionAsync(() =>
        {
            var titleCell = component.Find("table.grid-data tbody tr td:nth-child(5)");
            titleCell.TextContent.ShouldBe(new string('A', 60) + "…");
            titleCell.GetAttribute("title").ShouldBe(fullTitle);
        });
    }

    [Test]
    public async Task ShouldShowShortTitleUnchangedWithoutTooltip()
    {
        const string shortTitle = "Fix leaky faucet";
        var workOrder = new WorkOrder
        {
            Number = "WO-SHT",
            Title = shortTitle
        };
        await using var ctx = CreateContext(new StubBus([workOrder]));
        var component = ctx.Render<WorkOrderSearch>();
        await component.WaitForAssertionAsync(() =>
        {
            var titleCell = component.Find("table.grid-data tbody tr td:nth-child(5)");
            titleCell.TextContent.ShouldBe(shortTitle);
            titleCell.GetAttribute("title").ShouldBeNull();
        });
    }

    private static BunitContext CreateContext(IBus bus)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton(new WorkOrderSearchState());
        ctx.AddAuthorization().SetAuthorized("defaultuser");
        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync()
        {
            return Task.FromResult<Employee?>(new Employee("defaultuser", "Test", "User", "test@example.com"));
        }
    }
}
