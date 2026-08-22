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
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageInstructionsTests
{
    [Test]
    public void WorkOrderManage_ShouldRenderInstructionsAsTextareaBetweenDescriptionAndRoom()
    {
        using var ctx = new TestContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        creator.Id = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        var description = component.WaitForElement($"[data-testid='{WorkOrderManage.Elements.Description}']");
        var instructions = component.WaitForElement($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
        var room = component.WaitForElement($"[data-testid='{WorkOrderManage.Elements.RoomNumber}']");

        instructions.TagName.ShouldBe("TEXTAREA");
        instructions.GetAttribute("id").ShouldBe("Instructions");

        var formGrid = component.Find(".form-grid");
        var fieldOrder = formGrid.Children
            .Where(element => element.ClassList.Contains("form-group"))
            .Select(element => element.QuerySelector("[data-testid]")?.GetAttribute("data-testid"))
            .Where(testId => testId is not null)
            .ToList();

        var descriptionIndex = fieldOrder.IndexOf(nameof(WorkOrderManage.Elements.Description));
        var instructionsIndex = fieldOrder.IndexOf(nameof(WorkOrderManage.Elements.Instructions));
        var roomIndex = fieldOrder.IndexOf(nameof(WorkOrderManage.Elements.RoomNumber));

        descriptionIndex.ShouldBeGreaterThan(-1);
        instructionsIndex.ShouldBeGreaterThan(descriptionIndex);
        roomIndex.ShouldBeGreaterThan(instructionsIndex);
    }

    [Test]
    public void WorkOrderManage_ShouldDisableInstructionsWhenReadOnly()
    {
        using var ctx = new TestContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        creator.Id = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-INST",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Title = "Completed",
            Description = "Done",
            Instructions = "Bring ladder and safety gear"
        };

        ctx.Services.AddSingleton<IBus>(new StubReadOnlyBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrder.Id, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{workOrder.Number}?mode=Edit");

        var component = ctx.RenderComponent<WorkOrderManage>(parameters =>
            parameters.Add(component => component.Id, workOrder.Number));

        var instructions = component.WaitForElement($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
        instructions.HasAttribute("disabled").ShouldBeTrue();
        instructions.GetAttribute("value").ShouldBe("Bring ladder and safety gear");
    }

    private class StubWorkOrderManageBus : Bus
    {
        public StubWorkOrderManageBus() : base(null!)
        {
        }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubReadOnlyBus : Bus
    {
        private readonly WorkOrder _workOrder;

        public StubReadOnlyBus(WorkOrder workOrder) : base(null!)
        {
            _workOrder = workOrder;
        }
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)_workOrder);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
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
                Number = "WO-INST",
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
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
