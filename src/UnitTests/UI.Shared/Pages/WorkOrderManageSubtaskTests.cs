using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
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
public class WorkOrderManageSubtaskTests
{
    [Test]
    public void WorkOrderManage_ShouldRenderSubtaskChecklist()
    {
        using var ctx = new TestContext();
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-001",
            Title = "Test order",
            Description = "desc",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "j@e.com"),
            Assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "j@e.com")
        };
        workOrder.Subtasks.Add(new WorkOrderSubtask { Id = Guid.NewGuid(), WorkOrderId = workOrderId, Title = "Step 1", SortOrder = 0 });
        workOrder.Subtasks.Add(new WorkOrderSubtask { Id = Guid.NewGuid(), WorkOrderId = workOrderId, Title = "Step 2", SortOrder = 1, IsCompleted = true });

        ctx.Services.AddSingleton<IBus>(new WorkOrderManageStubBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(workOrder.Creator!));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrder.Creator!));

        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = nav.GetUriWithQueryParameter("mode", "edit");
        nav.NavigateTo(uri);

        var component = ctx.RenderComponent<WorkOrderManage>(p => p
            .Add(x => x.Id, "WO-001"));

        var checklist = component.Find($"[data-testid='{WorkOrderManage.Elements.SubtaskChecklist}']");
        checklist.ShouldNotBeNull();

        var items = component.FindAll($"[data-testid^='{WorkOrderManage.Elements.SubtaskItem}']");
        items.Count.ShouldBe(2);

        var progress = component.Find($"[data-testid='{WorkOrderManage.Elements.SubtaskProgress}']");
        progress.TextContent.ShouldContain("1 of 2 complete");
    }

    private sealed class WorkOrderManageStubBus(WorkOrder workOrder) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = new[] { workOrder.Creator! };
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrder);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            if (request is AddSubtaskCommand addCmd)
            {
                var subtask = new WorkOrderSubtask { Id = Guid.NewGuid(), WorkOrderId = addCmd.WorkOrderId, Title = addCmd.Title, SortOrder = addCmd.SortOrder };
                return Task.FromResult<TResponse>((TResponse)(object)subtask);
            }

            if (request is ToggleSubtaskCommand toggleCmd)
            {
                var subtask = new WorkOrderSubtask { Id = toggleCmd.SubtaskId, IsCompleted = true };
                return Task.FromResult<TResponse>((TResponse)(object)subtask);
            }

            if (request is RemoveSubtaskCommand)
            {
                return Task.FromResult<TResponse>((TResponse)(object)true);
            }

            throw new NotImplementedException($"Unhandled request: {request.GetType().Name}");
        }
    }

    private sealed class StubUserSession(Employee employee) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(employee);
    }

    private sealed class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee c) => new() { Creator = creator };
    }
}
