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
public class WorkOrderManageTests
{
    [Test]
    public void WorkOrderManage_ShouldRenderTemplateDropdown_OnNewWorkOrder()
    {
        using var ctx = new TestContext();

        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var templates = new[]
        {
            new WorkOrderTemplate
            {
                Id = Guid.NewGuid(),
                Title = "Weekly Bathroom Cleaning",
                Description = "Clean all bathrooms",
                RoomNumber = "B101",
                IsActive = true,
                CreatedById = currentUser.Id,
                CreatedDate = DateTime.UtcNow
            }
        };

        var stubBus = new StubWorkOrderManageBus(currentUser, templates);
        var stubUserSession = new StubWorkOrderManageUserSession(currentUser);
        var stubWorkOrderBuilder = new StubWorkOrderBuilder(currentUser);

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(stubWorkOrderBuilder);

        var navManager = ctx.Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo(navManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        var templateSelect = component.FindAll($"[data-testid='{WorkOrderManage.Elements.TemplateSelect}']");
        templateSelect.Count.ShouldBe(1);

        var options = templateSelect[0].QuerySelectorAll("option");
        options.Length.ShouldBeGreaterThan(1);
        options[1].TextContent.ShouldBe("Weekly Bathroom Cleaning");
    }

    private class StubWorkOrderManageUserSession(Employee currentUser) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(currentUser);
    }

    private class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee employee)
        {
            return new WorkOrder
            {
                Number = "WO-001",
                Creator = creator,
                Status = WorkOrderStatus.Draft
            };
        }
    }

    private class StubWorkOrderManageBus(Employee currentUser, WorkOrderTemplate[] templates) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = new[] { currentUser };
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is WorkOrderTemplatesQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)templates);
            }

            throw new NotImplementedException($"No stub handler for {request.GetType().Name}");
        }
    }
}
