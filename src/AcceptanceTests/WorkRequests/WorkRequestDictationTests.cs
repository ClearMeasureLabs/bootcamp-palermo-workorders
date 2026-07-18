using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestDictationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldRenderDictateTitleButtonOnWorkRequestManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        var dictateTitleButton = Page.GetByTestId(nameof(WorkRequestManage.Elements.DictateTitle));
        await Expect(dictateTitleButton).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldRenderDictateDescriptionButtonOnWorkRequestManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        var dictateDescriptionButton = Page.GetByTestId(nameof(WorkRequestManage.Elements.DictateDescription));
        await Expect(dictateDescriptionButton).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClickDictateTitleButtonWithoutError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Click(nameof(WorkRequestManage.Elements.DictateTitle));

        await Expect(Page).ToHaveURLAsync(new Regex("/workrequest/manage/"));
    }

    [Test, Retry(2)]
    public async Task ShouldClickDictateDescriptionButtonWithoutError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Click(nameof(WorkRequestManage.Elements.DictateDescription));

        await Expect(Page).ToHaveURLAsync(new Regex("/workrequest/manage/"));
    }

    [Test, Retry(2)]
    public async Task ShouldHideDictateButtonsOnReadOnlyWorkRequest()
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
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.DictateTitle))).ToBeHiddenAsync();
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.DictateDescription))).ToBeHiddenAsync();
    }
}
