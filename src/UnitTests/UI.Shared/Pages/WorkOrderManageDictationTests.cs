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
using Toolbelt.Blazor.SpeechRecognition;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageDictationTests
{
    [Test]
    public async Task ShouldRenderDictateTitleButton()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public async Task ShouldRenderDictateDescriptionButton()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateDescription}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public async Task DictateTitleButtonShouldShowListeningStateWhenClicked()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        var dictateButton = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
        dictateButton.Click();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("true");
            element.GetAttribute("class").ShouldNotBeNull().ShouldContain("btn-danger");
        });

        component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']").Click();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("false");
            element.GetAttribute("class").ShouldNotBeNull().ShouldContain("btn-outline-secondary");
        });
    }

    [Test]
    public async Task ShouldAppendFinalTranscriptToTitleWhenDictating()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']").Click();
        await component.InvokeAsync(() =>
            component.Instance.SpeechRecognition!._OnResult(CreateFinalResult("broken window")));

        component.WaitForAssertion(() =>
        {
            component.Instance.Model.Title.ShouldBe("Test title broken window");
        });
    }

    [Test]
    public async Task ShouldSetDescriptionFromTranscriptWhenFieldIsEmpty()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        component.Find($"[data-testid='{WorkOrderManage.Elements.DictateDescription}']").Click();
        await component.InvokeAsync(() =>
            component.Instance.SpeechRecognition!._OnResult(CreateFinalResult("broken window")));

        component.WaitForAssertion(() =>
        {
            component.Instance.Model.Description.ShouldBe("broken window");
        });
    }

    [Test]
    public async Task ShouldIgnoreInterimResultsWhenDictating()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']").Click();
        var interimResult = new SpeechRecognitionEventArgs
        {
            ResultIndex = 0,
            Results =
            [
                new SpeechRecognitionResult
                {
                    IsFinal = false,
                    Items = [new SpeechRecognitionAlternative { Transcript = "broken window", Confidence = 0.5 }]
                }
            ]
        };
        await component.InvokeAsync(() =>
            component.Instance.SpeechRecognition!._OnResult(interimResult));

        component.WaitForAssertion(() =>
        {
            component.Instance.Model.Title.ShouldBe("Test title");
        });
    }

    [Test]
    public async Task ShouldResetListeningStateWhenRecognitionEnds()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']").Click();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("true");
        });

        await component.InvokeAsync(() => component.Instance.SpeechRecognition!._OnEnd());

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public async Task ShouldHideDictateButtonsWhenWorkOrderIsReadOnly()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };

        var creator = new Employee("someoneelse", "Someone", "Else", "se@example.com") { Id = Guid.NewGuid() };

        var completedWorkOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-DONE",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "Completed work order"
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(completedWorkOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, "WO-DONE"));

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']").ShouldNotBeNull();
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']").ShouldBeEmpty();
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.DictateDescription}']").ShouldBeEmpty();
        });
    }

    private static BunitContext CreateTestContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        {
            Id = Guid.NewGuid(),
            PreferredLanguage = "es-ES"
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        return ctx;
    }

    private static SpeechRecognitionEventArgs CreateFinalResult(string transcript)
    {
        return new SpeechRecognitionEventArgs
        {
            ResultIndex = 0,
            Results =
            [
                new SpeechRecognitionResult
                {
                    IsFinal = true,
                    Items = [new SpeechRecognitionAlternative { Transcript = transcript, Confidence = 0.9 }]
                }
            ]
        };
    }

    private class StubBus(WorkOrder? workOrderByNumber) : Bus(null!)
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
                var attachments = Array.Empty<WorkOrderAttachment>();
                return Task.FromResult<TResponse>((TResponse)(object)attachments);
            }

            if (request is WorkOrderByNumberQuery && workOrderByNumber != null)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrderByNumber);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-TEST",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Test title"
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
