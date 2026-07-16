using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDictationTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldRenderDictateTitleButtonOnWorkOrderManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var dictateTitleButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.DictateTitle));
        await Expect(dictateTitleButton).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldRenderDictateDescriptionButtonOnWorkOrderManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var dictateDescriptionButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.DictateDescription));
        await Expect(dictateDescriptionButton).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClickDictateTitleButtonWithoutError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Click(nameof(WorkOrderManage.Elements.DictateTitle));

        await Expect(Page).ToHaveURLAsync(new Regex("/workorder/manage/"));
    }

    [Test, Retry(2)]
    public async Task ShouldClickDictateDescriptionButtonWithoutError()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Click(nameof(WorkOrderManage.Elements.DictateDescription));

        await Expect(Page).ToHaveURLAsync(new Regex("/workorder/manage/"));
    }

    [Test, Retry(2)]
    public async Task ShouldHideDictateButtonsOnReadOnlyWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.ReadOnlyMessage))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.SpeakTitle))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DictateTitle))).ToBeHiddenAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DictateDescription))).ToBeHiddenAsync();
    }
}
