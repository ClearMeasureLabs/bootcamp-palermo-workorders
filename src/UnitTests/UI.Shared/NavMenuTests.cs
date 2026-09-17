using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavMenuTests
{
    [Test]
    public async Task ShouldShowRoomsNavItem_WhenCurrentUserCanCreateWorkOrders()
    {
        var leader = new Employee("mburns", "Montgomery", "Burns", "burns@plant.com");
        leader.Roles.Add(new Role("Leader", canCreate: true, canFulfill: false));
        await using var ctx = CreateContext(leader);

        var component = ctx.Render<NavMenu>();

        await component.WaitForAssertionAsync(() =>
        {
            component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Rooms)}']").Count.ShouldBe(1);
            component.FindAll($"[data-testid='{nameof(NavMenu.Elements.NewWorkOrder)}']").Count.ShouldBe(1);
        });
    }

    [Test]
    public async Task ShouldHideRoomsNavItem_WhenCurrentUserCannotCreateWorkOrders()
    {
        var technician = new Employee("hsimpson", "Homer", "Simpson", "homer@plant.com");
        technician.Roles.Add(new Role("Technician", canCreate: false, canFulfill: true));
        await using var ctx = CreateContext(technician);

        var component = ctx.Render<NavMenu>();

        await component.WaitForAssertionAsync(() =>
        {
            component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Search)}']").Count.ShouldBe(1);
            component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Rooms)}']").Count.ShouldBe(0);
            component.FindAll($"[data-testid='{nameof(NavMenu.Elements.NewWorkOrder)}']").Count.ShouldBe(0);
        });
    }

    [Test]
    public async Task ShouldHideRoomsNavItem_WhenNoUserIsLoggedIn()
    {
        await using var ctx = CreateContext(null);

        var component = ctx.Render<NavMenu>();

        component.FindAll($"[data-testid='{nameof(NavMenu.Elements.Rooms)}']").Count.ShouldBe(0);
    }

    private static BunitContext CreateContext(Employee? currentUser)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(currentUser));
        return ctx;
    }

    private sealed class StubUserSession(Employee? user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult(user);
    }
}
