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
public class WorkOrderManageInstructionTemplateTests
{
    [Test]
    public async Task WorkOrderManage_ShouldAppendInstructionTemplateToEmptyInstructions()
    {
        await using var ctx = CreateNewModeContext();
        var component = ctx.Render<WorkOrderManage>();
        var template = WorkOrderManage.InstructionTemplates[0];

        var select = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.InstructionTemplate}']");
        await select.ChangeAsync(new ChangeEventArgs { Value = template });

        component.Instance.Model.Instructions.ShouldBe(template);
        select = component.Find($"[data-testid='{WorkOrderManage.Elements.InstructionTemplate}']");
        select.GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task WorkOrderManage_ShouldAppendInstructionTemplateOnNewLineWhenInstructionsNotEmpty()
    {
        await using var ctx = CreateNewModeContext();
        var component = ctx.Render<WorkOrderManage>();
        component.Instance.Model.Instructions = "Existing line";
        var template = WorkOrderManage.InstructionTemplates[1];

        var select = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.InstructionTemplate}']");
        await select.ChangeAsync(new ChangeEventArgs { Value = template });

        component.Instance.Model.Instructions.ShouldBe($"Existing line\n{template}");
    }

    [Test]
    public async Task WorkOrderManage_ShouldHideInstructionTemplateWhenReadOnly()
    {
        await using var ctx = new BunitContext();

        var viewer = new Employee("viewer", "View", "Only", "viewer@example.com") { Id = Guid.NewGuid() };
        var creator = new Employee("creator", "Create", "Or", "creator@example.com") { Id = Guid.NewGuid() };
        var completedWorkOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-RO-TPL",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "Completed",
            Description = "Done",
            Instructions = "Saved guidance"
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(completedWorkOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(Guid.NewGuid(), creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(viewer));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, completedWorkOrder.Number));

        await component.WaitForElementAsync($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
        component.FindAll($"[data-testid='{WorkOrderManage.Elements.InstructionTemplate}']")
            .Count.ShouldBe(0);
    }

    [Test]
    public void AppendInstructionTemplate_ShouldNotExceedMaxLength()
    {
        var existing = new string('x', WorkOrder.InstructionsMaxLength - 5);
        var result = WorkOrderManage.AppendInstructionTemplate(existing, "Wear gloves and eye protection.");

        result.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
        result.ShouldStartWith(existing);
    }

    private static BunitContext CreateNewModeContext()
    {
        var ctx = new BunitContext();
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

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
                Number = "WO-TPL",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Template field"
            };
        }
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode)
        {
            return Task.FromResult(text);
        }
    }
}
