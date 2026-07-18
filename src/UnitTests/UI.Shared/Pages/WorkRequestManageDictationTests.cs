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
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkRequestManageDictationTests
{
    [Test]
    public void ShouldRenderDictateTitleButton()
    {
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public void ShouldRenderDictateDescriptionButton()
    {
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateDescription}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public void DictateTitleButtonShouldShowListeningStateWhenClicked()
    {
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        var dictateButton = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']");
        dictateButton.Click();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("true");
            element.GetAttribute("class").ShouldNotBeNull().ShouldContain("btn-danger");
        });

        component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']").Click();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("false");
            element.GetAttribute("class").ShouldNotBeNull().ShouldContain("btn-outline-secondary");
        });
    }

    [Test]
    public async Task ShouldAppendFinalTranscriptToTitleWhenDictating()
    {
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']").Click();
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
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.Find($"[data-testid='{WorkRequestManage.Elements.DictateDescription}']").Click();
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
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']").Click();
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
        using var ctx = CreateTestContext(out _);

        var component = ctx.RenderComponent<WorkRequestManage>();

        component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']").Click();

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("true");
        });

        await component.InvokeAsync(() => component.Instance.SpeechRecognition!._OnEnd());

        component.WaitForAssertion(() =>
        {
            var element = component.Find($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public void ShouldHideDictateButtonsWhenWorkRequestIsReadOnly()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        var creator = new Employee("someoneelse", "Someone", "Else", "se@example.com");
        creator.Id = Guid.NewGuid();

        var completedWorkRequest = new WorkRequest
        {
            Id = Guid.NewGuid(),
            Number = "WO-DONE",
            Status = WorkRequestStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "Completed work request"
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(completedWorkRequest));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkRequestBuilder>(new StubWorkRequestBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.RenderComponent<WorkRequestManage>(parameters =>
            parameters.Add(p => p.Id, "WO-DONE"));

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{WorkRequestManage.Elements.SpeakTitle}']").ShouldNotBeNull();
            component.FindAll($"[data-testid='{WorkRequestManage.Elements.DictateTitle}']").ShouldBeEmpty();
            component.FindAll($"[data-testid='{WorkRequestManage.Elements.DictateDescription}']").ShouldBeEmpty();
        });
    }

    private static TestContext CreateTestContext(out Employee user)
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();
        user.PreferredLanguage = "es-ES";

        ctx.Services.AddSingleton<IBus>(new StubBus(null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkRequestBuilder>(new StubWorkRequestBuilder());
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

    private class StubBus(WorkRequest? workRequestByNumber) : Bus(null!)
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
                var attachments = Array.Empty<WorkRequestAttachment>();
                return Task.FromResult<TResponse>((TResponse)(object)attachments);
            }

            if (request is WorkRequestByNumberQuery && workRequestByNumber != null)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workRequestByNumber);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkRequestBuilder : IWorkRequestBuilder
    {
        public WorkRequest CreateNewWorkRequest(Employee creator)
        {
            return new WorkRequest
            {
                Id = Guid.NewGuid(),
                Number = "WO-TEST",
                Status = WorkRequestStatus.Draft,
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
