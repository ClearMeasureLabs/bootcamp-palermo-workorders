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
public class WorkOrderManageCreatorTests
{
    [Test]
    public async Task Creator_ShouldDisplayFormattedFullName_ForTimothyLovejoyJr()
    {
        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "tl@example.com") { Id = Guid.NewGuid() };
        (await RenderCreatorSpanAsync(creator)).ShouldBe("TIMOTHY LOVEJOY JR");
    }

    [Test]
    public async Task Creator_ShouldDisplayFormattedFullName_ForHelenLovejoy()
    {
        var creator = new Employee("hlovejoy", "Helen", "Lovejoy", "hl@example.com") { Id = Guid.NewGuid() };
        (await RenderCreatorSpanAsync(creator)).ShouldBe("HELEN LOVEJOY");
    }

    [Test]
    public async Task Creator_ShouldDisplayFormattedFullName_ForNedFlanders()
    {
        var creator = new Employee("nflanders", "Ned", "Flanders", "nf@example.com") { Id = Guid.NewGuid() };
        (await RenderCreatorSpanAsync(creator)).ShouldBe("NED FLANDERS");
    }

    [Test]
    public async Task Creator_ShouldDisplayFormattedFullName_ForJeffreyPalermo()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        (await RenderCreatorSpanAsync(creator)).ShouldBe("JEFFREY PALERMO");
    }

    private static async Task<string> RenderCreatorSpanAsync(Employee creator)
    {
        await using var ctx = new BunitContext();

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

        var component = ctx.Render<WorkOrderManage>();

        var span = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.Creator}']");
        return span.TextContent.Trim();
    }

    private class StubWorkOrderManageBus : Bus
    {
        // ReSharper disable once ConvertToPrimaryConstructor -- left as classic ctor by Qodana policy
        public StubWorkOrderManageBus() : base(null!)
        {
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
                Number = "WO-CREATOR",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Creator field"
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
