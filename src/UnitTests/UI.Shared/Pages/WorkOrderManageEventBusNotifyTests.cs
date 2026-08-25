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
public class WorkOrderManageEventBusNotifyTests
{
    [Test]
    public void ShouldCountNonGenericNotify_WhenNotifiedThroughTheObjectOverload()
    {
        var uiBus = new SpyUiBus();
        var workOrder = new WorkOrder { Id = Guid.NewGuid(), Number = "WO-TEST" };

        uiBus.Notify((object)new WorkOrderSelectedEvent(workOrder));

        uiBus.NonGenericNotifyCount.ShouldBe(1);
        uiBus.NotifiedWorkOrderSelectedCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldNotRenotifyWorkOrderSelectedOnEveryRerender_WhenWorkOrderIsUnchanged()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        var uiBus = new SpyUiBus();
        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(uiBus);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        component.WaitForAssertion(() => uiBus.NotifiedWorkOrderSelectedCount.ShouldBe(1));
        var countAfterInitialLoad = uiBus.NotifiedWorkOrderSelectedCount;

        var titleInput = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
        titleInput.Change("First edit forces a re-render");
        titleInput.Change("Second edit forces another re-render");

        uiBus.NotifiedWorkOrderSelectedCount.ShouldBe(countAfterInitialLoad,
            "typing into the Title field re-renders the page but must not re-notify " +
            "WorkOrderSelectedEvent for the SAME work order instance on every render; " +
            "that unconditional notify forces listeners like WorkOrderChat to reset their own state.");
    }

    private class SpyUiBus : IUiBus
    {
        public int NotifiedWorkOrderSelectedCount { get; private set; }
        public int NonGenericNotifyCount { get; private set; }

        public void Notify(object eventObject)
        {
            NonGenericNotifyCount++;
            if (eventObject is WorkOrderSelectedEvent)
            {
                NotifiedWorkOrderSelectedCount++;
            }
        }

        public void Register(IListener listener)
        {
        }

        public void UnRegister(IListener listener)
        {
        }

        public IListener<T>[] GetListeners<T>() where T : IUiBusEvent
        {
            return Array.Empty<IListener<T>>();
        }

        public void Notify<T>(T eventObject) where T : IUiBusEvent
        {
            if (eventObject is WorkOrderSelectedEvent)
            {
                NotifiedWorkOrderSelectedCount++;
            }
        }

        public void UnRegisterAll()
        {
        }
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
                var employees = Array.Empty<Employee>();
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid workOrderId) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-TEST",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Test Order"
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
