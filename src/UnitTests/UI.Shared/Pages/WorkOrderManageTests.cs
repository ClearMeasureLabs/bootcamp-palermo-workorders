using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageTests
{
    [Test]
    public void WorkOrderManage_ShouldRenderBuildingInput()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubBusForManage());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());

        var component = ctx.RenderComponent<WorkOrderManage>();

        var buildingInput = component.Find($"[data-testid='{WorkOrderManage.Elements.Building}']");
        buildingInput.ShouldNotBeNull();
    }

    [Test]
    public void WorkOrderManage_ShouldRenderFloorInput()
    {
        using var ctx = new TestContext();

        ctx.Services.AddSingleton<IBus>(new StubBusForManage());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());

        var component = ctx.RenderComponent<WorkOrderManage>();

        var floorInput = component.Find($"[data-testid='{WorkOrderManage.Elements.Floor}']");
        floorInput.ShouldNotBeNull();
    }

    private class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync()
        {
            var employee = new Employee("testuser", "Test", "User", "test@example.com");
            employee.AddRole(new Role("admin", true, true));
            return Task.FromResult<Employee?>(employee);
        }
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Number = "WO-001",
                Title = "",
                Description = "",
                Status = WorkOrderStatus.Draft,
                Creator = creator
            };
        }
    }

    private class StubBusForManage() : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = new[]
                {
                    new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com"),
                };
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            throw new NotImplementedException($"StubBusForManage does not handle {request.GetType().Name}");
        }
    }
}
