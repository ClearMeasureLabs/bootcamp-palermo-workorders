using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
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
    [Test]
    public void WorkOrderManage_ShouldRenderSlaFields()
    {
        using var ctx = new TestContext();

        var currentUser = new Employee("testuser", "Test", "User", "test@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-001",
            Creator = currentUser,
            Status = WorkOrderStatus.Draft
        };

        ctx.Services.AddSingleton<IBus>(new WorkOrderManageStubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(currentUser));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrder));

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Mode", "New");
        navigationManager.NavigateTo(uri);

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.Find($"[data-testid='{WorkOrderManage.Elements.SlaResponseHours}']").ShouldNotBeNull();
        component.Find($"[data-testid='{WorkOrderManage.Elements.SlaResolutionHours}']").ShouldNotBeNull();
    }

    private sealed class WorkOrderManageStubBus() : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<Employee>());

            throw new NotImplementedException($"No handler for {request.GetType().Name}");
        }
    }

    private sealed class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubWorkOrderBuilder(WorkOrder workOrder) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator) => workOrder;
    }
}
