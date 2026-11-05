using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageCreatorDisplayTests
{
    [Test]
    public async Task ShouldDisplayCreatorInMixedCase_WhenNewWorkOrderRendered()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "reverend@firstchurchspringfield.org") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            // The creator form-group is the first form-group in the form-grid; its span.value holds the creator name.
            var creatorGroup = component.Find("div.form-grid > div.form-group");
            var creatorSpan = creatorGroup.QuerySelector("span.value");
            creatorSpan.ShouldNotBeNull();
            creatorSpan.TextContent.ShouldBe("Timothy Lovejoy Jr");
            creatorSpan.TextContent.ShouldNotBe("TIMOTHY LOVEJOY JR");
        });
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
