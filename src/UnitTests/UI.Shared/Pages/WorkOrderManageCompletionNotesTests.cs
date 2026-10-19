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
public class WorkOrderManageCompletionNotesTests
{
    [Test]
    public async Task WorkOrderManage_InProgress_Assignee_ShouldShowEditableCompletionNotes()
    {
        var assignee = new Employee("tech1", "Tech", "One", "tech@example.com") { Id = Guid.NewGuid() };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-CN01",
            Title = "Fix filter",
            Status = WorkOrderStatus.InProgress,
            Assignee = assignee,
            Creator = assignee
        };

        await using var ctx = CreateEditContext(workOrder, assignee);

        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, workOrder.Number));

        var notes = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CompletionNotes}']");
        notes.TagName.ShouldBe("TEXTAREA");
        notes.HasAttribute("disabled").ShouldBeFalse();
        notes.GetAttribute("maxlength").ShouldBe(WorkOrder.CompletionNotesMaxLength.ToString());
    }

    [Test]
    public async Task WorkOrderManage_Complete_ShouldShowReadOnlyCompletionNotes()
    {
        var assignee = new Employee("tech1", "Tech", "One", "tech@example.com") { Id = Guid.NewGuid() };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-CN02",
            Title = "Fix filter",
            Status = WorkOrderStatus.Complete,
            Assignee = assignee,
            Creator = assignee,
            CompletionNotes = "Replaced the filter"
        };

        await using var ctx = CreateEditContext(workOrder, assignee);

        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, workOrder.Number));

        var notes = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CompletionNotes}']");
        notes.TagName.ShouldBe("TEXTAREA");
        notes.HasAttribute("disabled").ShouldBeTrue();
        notes.GetAttribute("value")?.ShouldBe("Replaced the filter");
    }

    private static BunitContext CreateEditContext(WorkOrder workOrder, Employee currentUser)
    {
        var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrder.Id, currentUser));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(currentUser));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        return ctx;
    }

    private class StubWorkOrderManageBus(WorkOrder? workOrderByNumber = null) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult((TResponse)(object)employees);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            if (request is WorkOrderByNumberQuery && workOrderByNumber != null)
            {
                return Task.FromResult((TResponse)(object)workOrderByNumber);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid workOrderId, Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-NEW",
                Creator = creator,
                Status = WorkOrderStatus.Draft
            };
        }
    }

    private class StubUserSession(Employee currentUser) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(currentUser);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguage) => Task.FromResult(text);
    }
}
