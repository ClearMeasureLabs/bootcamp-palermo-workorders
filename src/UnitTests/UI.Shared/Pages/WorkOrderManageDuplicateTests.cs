using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
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
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.SpeechRecognition;
using Toolbelt.Blazor.SpeechSynthesis;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageDuplicateTests
{
    [Test]
    public async Task ShouldShowDuplicateButton_WhenEditMode()
    {
        await using var ctx = CreateContext(out var order);
        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(nav.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, order.Number));

        await component.WaitForAssertionAsync(() =>
        {
            var button = component.Find($"[data-testid='{nameof(WorkOrderManage.Elements.Duplicate)}']");
            button.ShouldNotBeNull();
            button.TextContent.Trim().ShouldBe("Duplicate");
        });
    }

    [Test]
    public async Task ShouldPrefillNewDraft_WhenDuplicateClicked()
    {
        await using var ctx = CreateContext(out var order);
        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(nav.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, order.Number));

        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{nameof(WorkOrderManage.Elements.Duplicate)}']"));

        await component.Find($"[data-testid='{nameof(WorkOrderManage.Elements.Duplicate)}']")
            .ClickAsync();

        await component.WaitForAssertionAsync(() =>
        {
            component.Instance.Model.Mode.ShouldBe(EditMode.New);
            component.Instance.Model.Title.ShouldBe("Copy of Original Title");
            component.Instance.Model.Description.ShouldBe("Original Description");
            component.Instance.Model.Instructions.ShouldBe("Original Instructions");
            component.Instance.Model.RoomNumber.ShouldBe("101");
            component.Instance.Model.AssignedToUserName.ShouldBeNull();
            component.Instance.Model.DueDateInput.ShouldBeNull();
            component.Instance.Model.WorkOrderNumber.ShouldNotBe(order.Number);
            component.Instance.Model.Status.ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        });

        component.FindAll($"[data-testid='{nameof(WorkOrderManage.Elements.Duplicate)}']")
            .Count.ShouldBe(0);
    }

    private static BunitContext CreateContext(out WorkOrder order)
    {
        var creator = new Employee("creator", "Create", "Or", "c@example.com");
        order = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Original Title",
            Description = "Original Description",
            Instructions = "Original Instructions",
            RoomNumber = "101",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = new Employee("assignee", "As", "Signee", "a@example.com"),
            DueDate = new DateOnly(2030, 1, 15)
        };

        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new DuplicateStubBus(order));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();
        ctx.Services.AddSingleton(TimeProvider.System);
        return ctx;
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguage) =>
            Task.FromResult(text);
    }

    private sealed class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator) =>
            new()
            {
                Number = "WO-DUP",
                Status = WorkOrderStatus.Draft,
                Creator = creator
            };
    }

    private sealed class StubUserSession(Employee currentUser) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(currentUser);
    }

    private sealed class DuplicateStubBus(WorkOrder order) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)order);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }
}
