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
public class WorkOrderManageAttachmentsTests
{
    [Test]
    public void WorkOrderManage_ShouldRenderAttachmentsSection()
    {
        using var ctx = new TestContext();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        uploader.Id = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var attachments = new[]
        {
            new WorkOrderAttachment
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                FileName = "damage-photo.jpg",
                ContentType = "image/jpeg",
                FileSize = 2048,
                UploadedById = uploader.Id,
                UploadedBy = uploader,
                UploadedDate = new DateTime(2025, 3, 1, 10, 0, 0)
            }
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(attachments));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(uploader));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var section = component.Find($"[data-testid='{WorkOrderManage.Elements.AttachmentsSection}']");
            section.ShouldNotBeNull();
        });

        var fileNameCell = component.Find($"[data-testid='{WorkOrderManage.Elements.AttachmentFileName}']");
        fileNameCell.TextContent.ShouldBe("damage-photo.jpg");
    }

    private class StubWorkOrderManageBus(WorkOrderAttachment[] attachments) : Bus(null!)
    {
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
                return Task.FromResult<TResponse>((TResponse)(object)attachments);
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
