using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
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
public class WorkOrderPrintTests
{
    private static readonly Employee Creator = new("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
    private static readonly Employee Assignee = new("hsimpson", "Homer", "Simpson", "homer@springfield.com");

    private static WorkOrder BuildWorkOrder() => new()
    {
        Number = "WO-001",
        Title = "Fix broken door",
        Description = "The door hinge is broken and needs replacement.",
        RoomNumber = "101",
        Status = WorkOrderStatus.Assigned,
        Creator = Creator,
        Assignee = Assignee,
        CreatedDate = new DateTime(2025, 1, 15, 10, 0, 0),
        AssignedDate = new DateTime(2025, 1, 16, 9, 0, 0),
        CompletedDate = null
    };

    [Test]
    public void WorkOrderPrint_ShouldRenderAllWorkOrderFields()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("window.print", _ => true);
        ctx.Services.AddSingleton<IBus>(new StubPrintBus(BuildWorkOrder()));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderPrint>(p => p.Add(x => x.Id, "WO-001"));

        component.Find($"[data-testid='{WorkOrderPrint.Elements.WorkOrderNumber}']").TextContent.ShouldBe("WO-001");
        component.Find($"[data-testid='{WorkOrderPrint.Elements.Status}']").TextContent.ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
        component.Find($"[data-testid='{WorkOrderPrint.Elements.Title}']").TextContent.ShouldBe("Fix broken door");
        component.Find($"[data-testid='{WorkOrderPrint.Elements.Description}']").TextContent.ShouldBe("The door hinge is broken and needs replacement.");
        component.Find($"[data-testid='{WorkOrderPrint.Elements.RoomNumber}']").TextContent.ShouldBe("101");
        component.Find($"[data-testid='{WorkOrderPrint.Elements.Creator}']").TextContent.ShouldBe("Jeffrey Palermo");
        component.Find($"[data-testid='{WorkOrderPrint.Elements.Assignee}']").TextContent.ShouldBe("Homer Simpson");
        component.Find($"[data-testid='{WorkOrderPrint.Elements.CreatedDate}']").TextContent.ShouldNotBeNullOrEmpty();
        component.Find($"[data-testid='{WorkOrderPrint.Elements.AssignedDate}']").TextContent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void WorkOrderPrint_ShouldRenderSignatureLine()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("window.print", _ => true);
        ctx.Services.AddSingleton<IBus>(new StubPrintBus(BuildWorkOrder()));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkOrderPrint>(p => p.Add(x => x.Id, "WO-001"));

        component.Find($"[data-testid='{WorkOrderPrint.Elements.SignatureLine}']").ShouldNotBeNull();
    }

    [Test]
    public void WorkOrderManage_ShouldRenderPrintButton()
    {
        using var ctx = new TestContext();
        ctx.Services.AddSingleton<IBus>(new StubManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Mode", "edit");
        navigationManager.NavigateTo(uri);

        var component = ctx.RenderComponent<WorkOrderManage>(p =>
        {
            p.Add(x => x.Id, "WO-001");
        });

        component.Find($"[data-testid='{WorkOrderManage.Elements.PrintButton}']").ShouldNotBeNull();
    }

    private class StubPrintBus(WorkOrder workOrder) : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrder);
            }

            throw new NotImplementedException();
        }
    }

    private class StubManageBus() : Bus(null!)
    {
        private readonly WorkOrder _workOrder = new()
        {
            Number = "WO-001",
            Title = "Fix broken door",
            Description = "Description",
            RoomNumber = "101",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com"),
            CreatedDate = DateTime.UtcNow
        };

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)_workOrder);
            }

            if (request is EmployeeGetAllQuery)
            {
                var employees = new[]
                {
                    new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
                };
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            throw new NotImplementedException();
        }
    }

    private class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync()
        {
            return Task.FromResult<Employee?>(new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com"));
        }
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Number = new WorkOrderNumberGenerator().GenerateNumber(),
                Creator = creator,
                Status = WorkOrderStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };
        }
    }
}
