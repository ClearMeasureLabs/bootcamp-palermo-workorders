using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageTests
{
    private static readonly Employee CurrentUser = new("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");

    [Test]
    public void WorkOrderManage_ShouldRenderEstimatedCostInput()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(new WorkOrder
        {
            Number = "WO-001",
            Status = WorkOrderStatus.Draft,
            Creator = CurrentUser
        }));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());

        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/workorder/manage/WO-001?mode=Edit");

        var component = ctx.RenderComponent<WorkOrderManage>(p =>
            p.Add(m => m.Id, "WO-001"));

        var input = component.Find($"[data-testid='{WorkOrderManage.Elements.EstimatedCost}']");
        input.ShouldNotBeNull();
    }

    [Test]
    public void WorkOrderManage_ShouldDisableActualCostInput_WhenDraft()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(new WorkOrder
        {
            Number = "WO-001",
            Status = WorkOrderStatus.Draft,
            Creator = CurrentUser
        }));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());

        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/workorder/manage/WO-001?mode=Edit");

        var component = ctx.RenderComponent<WorkOrderManage>(p =>
            p.Add(m => m.Id, "WO-001"));

        var input = component.Find($"[data-testid='{WorkOrderManage.Elements.ActualCost}']");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    private class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() =>
            Task.FromResult<Employee?>(CurrentUser);
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator) =>
            new() { Creator = creator, Status = WorkOrderStatus.Draft };
    }

    private class StubWorkOrderManageBus(WorkOrder workOrder) : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
                return Task.FromResult<TResponse>((TResponse)(object)workOrder);
            if (request is EmployeeGetAllQuery)
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<Employee>());
            throw new NotImplementedException($"No stub for {request.GetType().Name}");
        }
    }
}
