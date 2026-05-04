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
    public void WorkOrderManage_ShouldRenderInstructionsField_WhenEditable()
    {
        using var ctx = new TestContext();
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-99",
            Title = "T",
            Description = "D",
            Instructions = "Wear PPE in the server room.",
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(Guid.NewGuid()));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var targetUri = navigationManager.ToAbsoluteUri($"workorder/manage/{workOrder.Number}?mode=edit");
        navigationManager.NavigateTo(targetUri.ToString());

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            component.Instance.Model.Instructions.ShouldBe("Wear PPE in the server room.");
        });

        var instructionsArea = component.Find($"textarea[data-testid='{WorkOrderManage.Elements.Instructions}']");
        instructionsArea.ShouldNotBeNull();
    }

    private class StubBus(WorkOrder workOrder) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrder);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new InvalidOperationException($"Unexpected request: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid id) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder { Id = id, Creator = creator, Status = WorkOrderStatus.Draft };
        }
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguage) => Task.FromResult(text);
    }
}
