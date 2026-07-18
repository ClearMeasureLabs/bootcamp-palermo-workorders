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
public class WorkRequestManageAttachmentsTests
{
    [Test]
    public void WorkRequestManage_ShouldRenderAttachmentsSection()
    {
        using var ctx = new TestContext();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        uploader.Id = Guid.NewGuid();
        var workRequestId = Guid.NewGuid();

        var attachments = new[]
        {
            new WorkRequestAttachment
            {
                Id = Guid.NewGuid(),
                WorkRequestId = workRequestId,
                FileName = "damage-photo.jpg",
                ContentType = "image/jpeg",
                FileSize = 2048,
                UploadedById = uploader.Id,
                UploadedBy = uploader,
                UploadedDate = new DateTime(2025, 3, 1, 10, 0, 0)
            }
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkRequestManageBus(attachments));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkRequestBuilder>(new StubWorkRequestBuilder(workRequestId));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(uploader));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.WaitForAssertion(() =>
        {
            var section = component.Find($"[data-testid='{WorkRequestManage.Elements.AttachmentsSection}']");
            section.ShouldNotBeNull();
        });

        var fileNameCell = component.Find($"[data-testid='{WorkRequestManage.Elements.AttachmentFileName}']");
        fileNameCell.TextContent.ShouldBe("damage-photo.jpg");
    }

    private class StubWorkRequestManageBus(WorkRequestAttachment[] attachments) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is WorkRequestAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)attachments);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkRequestBuilder(Guid workRequestId) : IWorkRequestBuilder
    {
        public WorkRequest CreateNewWorkRequest(Employee creator)
        {
            return new WorkRequest
            {
                Id = workRequestId,
                Number = "WO-TEST",
                Status = WorkRequestStatus.Draft,
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
