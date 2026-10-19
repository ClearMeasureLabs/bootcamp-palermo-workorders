using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
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
public class WorkOrderManageAttachmentCountTests
{
    [Test]
    public async Task ShouldShowNoAttachmentsYet_WhenSavedWorkOrderHasZeroAttachments()
    {
        await using var ctx = CreateContext(attachments: []);
        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.AttachmentsSection}']")
                .TextContent.ShouldContain("Attachments (0)");
            component.Find($"[data-testid='{WorkOrderManage.Elements.NoAttachmentsMessage}']")
                .TextContent.ShouldContain("No attachments yet.");
        });
    }

    [Test]
    public async Task ShouldShowAttachmentCount_WhenSavedWorkOrderHasTwoAttachments()
    {
        var attachments = new[]
        {
            new WorkOrderAttachment { FileName = "a.pdf", ContentType = "application/pdf", FileSize = 100 },
            new WorkOrderAttachment { FileName = "b.jpg", ContentType = "image/jpeg", FileSize = 200 }
        };
        await using var ctx = CreateContext(attachments);
        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.AttachmentsSection}']")
                .TextContent.ShouldContain("Attachments (2)");
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.AttachmentRow}']").Count.ShouldBe(2);
        });
    }

    private static BunitContext CreateContext(WorkOrderAttachment[] attachments)
    {
        var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-ATT",
            Status = WorkOrderStatus.Draft,
            Creator = user,
            Title = "t",
            Description = "d"
        };
        var bus = new StubAttachmentCountBus(existing, attachments);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{existing.Number}?Mode=Edit");

        return ctx;
    }

    private sealed class StubAttachmentCountBus(WorkOrder existing, WorkOrderAttachment[] attachments) : Bus(null!)
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
                return Task.FromResult((TResponse)(object)attachments);
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

            if (request is IStateCommand)
            {
                var result = new StateCommandResult(existing);
                return Task.FromResult((TResponse)(object)result);
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
