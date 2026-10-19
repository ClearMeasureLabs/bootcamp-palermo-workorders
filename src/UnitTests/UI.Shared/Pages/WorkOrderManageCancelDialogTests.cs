using System.Reflection;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.SpeechRecognition;
using Toolbelt.Blazor.SpeechSynthesis;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageCancelDialogTests
{
    [Test]
    public async Task ShouldShowCancelConfirmation_WhenCancelClicked_AndConfirmSendsCancel()
    {
        await using var ctx = CreateContext(out var bus);
        var cut = RenderWithCancelCommand(ctx);

        await cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CommandButton)}{AssignedToCancelledCommand.Name}']")
            .ClickAsync();

        cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CancelConfirmationDialog)}']").ShouldNotBeNull();
        cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CancelConfirmationMessage)}']")
            .TextContent.ShouldContain("Cancel work order WO-CAN?");
        cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CancelConfirmationMessage)}']")
            .TextContent.ShouldContain("This removes the assignee.");

        var sendCountBeforeConfirm = bus.SendCallCount;
        await cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CancelConfirmButton)}']").ClickAsync();

        bus.SendCallCount.ShouldBeGreaterThan(sendCountBeforeConfirm);
        bus.LastCommandName.ShouldBe(AssignedToCancelledCommand.Name);
    }

    [Test]
    public async Task ShouldCloseCancelConfirmation_WhenKeepClicked_AndChangeNothing()
    {
        await using var ctx = CreateContext(out var bus);
        var cut = RenderWithCancelCommand(ctx);

        await cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CommandButton)}{AssignedToCancelledCommand.Name}']")
            .ClickAsync();
        cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CancelConfirmationDialog)}']").ShouldNotBeNull();

        var sendCountBeforeKeep = bus.SendCallCount;
        await cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CancelKeepButton)}']").ClickAsync();

        cut.FindAll($"[data-testid='{nameof(WorkOrderManage.Elements.CancelConfirmationDialog)}']").Count.ShouldBe(0);
        bus.SendCallCount.ShouldBe(sendCountBeforeKeep);
        bus.LastCommandName.ShouldBeNull();
    }

    private static BunitContext CreateContext(out CancelTrackingStubBus bus)
    {
        bus = new CancelTrackingStubBus();
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new SpeechRecognition(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(
            (SpeechSynthesis)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(SpeechSynthesis)));
        return ctx;
    }

    private static readonly Employee SharedFulfiller = CreateFulfiller("testuser");

    private static IRenderedComponent<WorkOrderManage> RenderWithCancelCommand(BunitContext ctx)
    {
        var navigationManager = (NavigationManager)ctx.Services.GetRequiredService(typeof(NavigationManager));
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var cut = ctx.Render<WorkOrderManage>(parameters => parameters.Add(p => p.Id, "WO-CAN"));

        var workOrder = CreateAssignedWorkOrder();
        var cancelCommand = new AssignedToCancelledCommand(workOrder, SharedFulfiller);

        cut.Instance.Model = new WorkOrderManageModel
        {
            WorkOrderNumber = "WO-CAN",
            Title = "Cancel me",
            Description = "Desc",
            AssignedToUserName = SharedFulfiller.UserName,
            Mode = EditMode.Edit,
            IsReadOnly = false,
            WorkOrder = workOrder
        };

        typeof(WorkOrderManage)
            .GetProperty("ValidCommands", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .SetValue(cut.Instance, (IEnumerable<IStateCommand>)[cancelCommand]);

        cut.Render();
        return cut;
    }

    private static Employee CreateFulfiller(string userName)
    {
        var employee = new Employee(userName, "Test", "User", $"{userName}@example.com");
        employee.Roles.Add(new Role { Name = "Fulfiller", CanFulfillWorkOrder = true, CanCreateWorkOrder = true });
        return employee;
    }

    private static WorkOrder CreateAssignedWorkOrder() =>
        new()
        {
            Number = "WO-CAN",
            Title = "Cancel me",
            Description = "Desc",
            Creator = SharedFulfiller,
            Assignee = SharedFulfiller,
            Status = WorkOrderStatus.Assigned
        };

    private sealed class CancelTrackingStubBus() : Bus(null!)
    {
        public int SendCallCount { get; private set; }
        public string? LastCommandName { get; private set; }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            SendCallCount++;
            if (request is IStateCommand command)
            {
                LastCommandName = command.TransitionVerbPresentTense;
                return Task.FromResult((TResponse)(object)new StateCommandResult(CreateAssignedWorkOrder()));
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)CreateAssignedWorkOrder());
            }

            if (request is EmployeeByUserNameQuery)
            {
                return Task.FromResult((TResponse)(object)SharedFulfiller);
            }

            if (request is EmployeeGetAllQuery)
            {
                Employee[] employees = [SharedFulfiller];
                return Task.FromResult((TResponse)(object)employees);
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync()
        {
            return Task.FromResult<Employee?>(SharedFulfiller);
        }
    }

    private sealed class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator) => new() { Creator = creator };
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguage) =>
            Task.FromResult(text);
    }
}
