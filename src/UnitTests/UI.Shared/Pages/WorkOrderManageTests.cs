using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkOrderManageTests
{
    [Test]
    public void WorkOrderManage_ShouldRenderReassignButton_WhenAssigned()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        creator.Id = Guid.NewGuid();
        var assignee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com");
        assignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder
        {
            Number = "WO-001",
            Title = "Test Order",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = assignee
        };

        using var ctx = new TestContext();
        var stubBus = new StubBus { WorkOrderByNumberResult = order };
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());

        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/workorder/manage/WO-001?mode=Edit");

        var component = ctx.RenderComponent<WorkOrderManage>(p => p
            .Add(c => c.Id, "WO-001"));

        var reassignButton = component.FindAll($"[data-testid='{WorkOrderManage.Elements.ReassignButton}']");
        reassignButton.Count.ShouldBe(1);
    }

    [Test]
    public void WorkOrderManage_ShouldHideReassignButton_WhenDraft()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        creator.Id = Guid.NewGuid();
        var order = new WorkOrder
        {
            Number = "WO-001",
            Title = "Test Order",
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };

        using var ctx = new TestContext();
        var stubBus = new StubBus { WorkOrderByNumberResult = order };
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());

        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/workorder/manage/WO-001?mode=Edit");

        var component = ctx.RenderComponent<WorkOrderManage>(p => p
            .Add(c => c.Id, "WO-001"));

        var reassignButton = component.FindAll($"[data-testid='{WorkOrderManage.Elements.ReassignButton}']");
        reassignButton.Count.ShouldBe(0);
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator) => new() { Creator = creator };
    }
}
