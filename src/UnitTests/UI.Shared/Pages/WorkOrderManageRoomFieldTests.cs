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
public class WorkOrderManageRoomFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderRoomAsInputSelect()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(Array.Empty<Room>()));
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

        var room = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.RoomNumber}']");
        room.TagName.ShouldBe("SELECT");
    }

    [Test]
    public async Task WorkOrderManage_ShouldPopulateRoomDropdownFromQuery()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        var room1 = new Room("101", "Conference Room A") { Id = Guid.NewGuid() };
        var room2 = new Room("202", "Sanctuary") { Id = Guid.NewGuid() };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(new[] { room1, room2 }));
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

        var select = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.RoomNumber}']");
        select.TagName.ShouldBe("SELECT");

        var options = select.QuerySelectorAll("option");
        var optionTexts = options.Select(o => o.TextContent).ToList();
        optionTexts.ShouldContain(room1.DisplayName);
        optionTexts.ShouldContain(room2.DisplayName);
    }

    private class StubWorkOrderManageBus(Room[] rooms) : Bus(null!)
    {
        // ReSharper disable once ConvertToPrimaryConstructor -- left as classic ctor by Qodana policy
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult((TResponse)(object)employees);
            }

            if (request is RoomGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)rooms);
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
                Number = "WO-ROOM",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Room field"
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
