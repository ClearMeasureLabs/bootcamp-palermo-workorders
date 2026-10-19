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
public class WorkOrderManageOpenWorkOrdersInRoomTests
{
    [Test]
    public async Task ShouldShowOpenWorkOrdersInRoom_WhenEditModeAndQueryReturnsAny()
    {
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-VIEW",
            Title = "Current",
            RoomNumber = "101",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        };
        var other = new WorkOrder
        {
            Number = "WO-OTH",
            Title = "Other open",
            RoomNumber = "101",
            Status = WorkOrderStatus.Assigned,
            Creator = existing.Creator
        };
        await using var ctx = CreateContext(existing, [other]);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-VIEW"));

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.OpenWorkOrdersInRoom}']")
                .TextContent.ShouldContain("Other open work orders in this room:");
            var link = component.Find($"[data-testid='{WorkOrderManage.Elements.OpenWorkOrderLink}WO-OTH']");
            link.TextContent.ShouldContain("WO-OTH");
            link.TextContent.ShouldContain("Other open");
            link.GetAttribute("href").ShouldBe("/workorder/manage/WO-OTH?Mode=Edit");
        });
    }

    [Test]
    public async Task ShouldNotShowOpenWorkOrdersInRoom_WhenQueryReturnsNone()
    {
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-VIEW",
            Title = "Current",
            RoomNumber = "101",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        };
        await using var ctx = CreateContext(existing, []);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-VIEW"));

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ShouldNotBeNull();
        });

        component.FindAll($"[data-testid='{WorkOrderManage.Elements.OpenWorkOrdersInRoom}']")
            .Count.ShouldBe(0);
    }

    private static BunitContext CreateContext(WorkOrder existing, WorkOrder[] openInRoom)
    {
        var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var bus = new StubOpenRoomBus(existing, openInRoom);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();
        return ctx;
    }

    private sealed class StubOpenRoomBus(WorkOrder existing, WorkOrder[] openInRoom) : Bus(null!)
    {
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

            if (request is OpenWorkOrdersInRoomQuery)
            {
                return Task.FromResult((TResponse)(object)openInRoom);
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)existing);
            }

            if (request is EmployeeByUserNameQuery byUser)
            {
                var employee = new Employee(byUser.Username, "A", "B", "a@b.com");
                return Task.FromResult((TResponse)(object)employee);
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee current)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-NEW",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "",
                Description = ""
            };
        }
    }

    private sealed class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
