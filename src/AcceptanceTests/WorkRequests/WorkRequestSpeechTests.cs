using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestSpeechTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldRenderSpeakTitleButtonOnWorkRequestManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        var speakTitleButton = Page.GetByTestId(nameof(WorkRequestManage.Elements.SpeakTitle));
        await Expect(speakTitleButton).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldRenderSpeakDescriptionButtonOnWorkRequestManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        var speakDescriptionButton = Page.GetByTestId(nameof(WorkRequestManage.Elements.SpeakDescription));
        await Expect(speakDescriptionButton).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClickSpeakTitleButtonWithoutError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Input(nameof(WorkRequestManage.Elements.Title), "Test speech title");
        await Click(nameof(WorkRequestManage.Elements.SpeakTitle));

        await Expect(Page).ToHaveURLAsync(new Regex("/workrequest/manage/"));
    }

    [Test, Retry(2)]
    public async Task ShouldClickSpeakDescriptionButtonWithoutError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Input(nameof(WorkRequestManage.Elements.Description), "Test speech description");
        await Click(nameof(WorkRequestManage.Elements.SpeakDescription));

        await Expect(Page).ToHaveURLAsync(new Regex("/workrequest/manage/"));
    }

    [Test, Retry(2)]
    public async Task ShouldShowSpeakButtonsOnReadOnlyWorkRequest()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await BeginExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await CompleteExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.ReadOnlyMessage))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.SpeakTitle))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.SpeakDescription))).ToBeVisibleAsync();
    }
}
