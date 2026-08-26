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
public class WorkOrderManageInstructionsFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderInstructionsAsTextareaWithMaxLength4000()
    {
        await using var ctx = CreateNewModeContext(out _);

        var component = ctx.Render<WorkOrderManage>();

        var instructions = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.Instructions}']");
        instructions.TagName.ShouldBe("TEXTAREA");
        instructions.GetAttribute("maxlength").ShouldBe(WorkOrder.InstructionsMaxLength.ToString());
    }

    [Test]
    public async Task WorkOrderManage_ShouldPlaceInstructionsBetweenDescriptionAndRoom()
    {
        await using var ctx = CreateNewModeContext(out _);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForElementAsync($"[data-testid='{WorkOrderManage.Elements.Instructions}']");

        var markup = component.Markup;
        var descriptionIndex = markup.IndexOf($"data-testid=\"{WorkOrderManage.Elements.Description}\"", StringComparison.Ordinal);
        var instructionsIndex = markup.IndexOf($"data-testid=\"{WorkOrderManage.Elements.Instructions}\"", StringComparison.Ordinal);
        var roomIndex = markup.IndexOf($"data-testid=\"{WorkOrderManage.Elements.RoomNumber}\"", StringComparison.Ordinal);

        descriptionIndex.ShouldBeGreaterThanOrEqualTo(0);
        instructionsIndex.ShouldBeGreaterThan(descriptionIndex);
        roomIndex.ShouldBeGreaterThan(instructionsIndex);
    }

    [Test]
    public async Task WorkOrderManage_ShouldDisableInstructionsWhenReadOnly()
    {
        await using var ctx = new BunitContext();

        var viewer = new Employee("viewer", "View", "Only", "viewer@example.com") { Id = Guid.NewGuid() };
        var creator = new Employee("creator", "Create", "Or", "creator@example.com") { Id = Guid.NewGuid() };
        var completedWorkOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-RO",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "Completed",
            Description = "Done",
            Instructions = "Saved guidance for viewers"
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

        var instructions = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.Instructions}']");
        instructions.HasAttribute("disabled").ShouldBeTrue();
        component.Instance.Model.Instructions.ShouldBe("Saved guidance for viewers");
    }

    private static BunitContext CreateNewModeContext(out Employee creator)
    {
        var ctx = new BunitContext();
        creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
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

    private class StubWorkOrderManageBus : Bus
    {
        private readonly WorkOrder? _workOrderByNumber;

        public StubWorkOrderManageBus(WorkOrder? workOrderByNumber = null) : base(null!)
        {
            _workOrderByNumber = workOrderByNumber;
        }

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

            if (request is WorkOrderByNumberQuery && _workOrderByNumber != null)
            {
                return Task.FromResult((TResponse)(object)_workOrderByNumber);
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
                Number = "WO-INSTR",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Instructions field"
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
