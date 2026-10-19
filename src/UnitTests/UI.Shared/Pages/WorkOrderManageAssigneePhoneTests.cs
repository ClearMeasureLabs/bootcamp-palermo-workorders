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
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageAssigneePhoneTests
{
    [Test]
    public async Task ShouldRenderAssigneePhoneAsTelLink_WhenPhoneNumberIsSet()
    {
        var assignee = new Employee("assignee1", "Ann", "Assignee", "ann@test.com")
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "555-0144"
        };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Status = WorkOrderStatus.Complete,
            Creator = assignee,
            Assignee = assignee,
            Title = "Done",
            Description = "Done"
        };

        await using var ctx = CreateReadOnlyContext(workOrder);
        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, workOrder.Number));

        var link = await component.WaitForElementAsync(
            $"[data-testid='{nameof(WorkOrderManage.Elements.AssigneePhone)}']");
        link.GetAttribute("href").ShouldBe("tel:555-0144");
        link.TextContent.ShouldBe("555-0144");
    }

    [Test]
    public async Task ShouldNotRenderAssigneePhone_WhenPhoneNumberIsNotSet()
    {
        var assignee = new Employee("assignee1", "Ann", "Assignee", "ann@test.com")
        {
            Id = Guid.NewGuid()
        };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-002",
            Status = WorkOrderStatus.Complete,
            Creator = assignee,
            Assignee = assignee,
            Title = "Done",
            Description = "Done"
        };

        await using var ctx = CreateReadOnlyContext(workOrder);
        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, workOrder.Number));

        await component.WaitForElementAsync($"[data-testid='{nameof(WorkOrderManage.Elements.AssigneeFullName)}']");

        component.FindAll($"[data-testid='{nameof(WorkOrderManage.Elements.AssigneePhone)}']")
            .Count.ShouldBe(0);
    }

    private static BunitContext CreateReadOnlyContext(WorkOrder workOrder)
    {
        var ctx = new BunitContext();
        var viewer = new Employee("viewer", "View", "Only", "viewer@example.com") { Id = Guid.NewGuid() };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(viewer));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        return ctx;
    }

    private class StubWorkOrderManageBus(WorkOrder workOrderByNumber) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());

            if (request is WorkOrderAttachmentsQuery)
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());

            if (request is WorkOrderByNumberQuery)
                return Task.FromResult((TResponse)(object)workOrderByNumber);

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser) =>
            new() { Number = "WO-NEW", Creator = currentUser, Status = WorkOrderStatus.Draft };
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
