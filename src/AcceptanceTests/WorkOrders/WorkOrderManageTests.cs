using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithTitleAt15Characters_Succeeds()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "123456789012345");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task CreateWorkOrder_WithTitleUnder15Characters_Succeeds()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Short Name");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "102");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task CreateWorkOrder_WithTitleOver15Characters_PreventedByUI()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await titleField.FillAsync("12345678901234567890");

        var actualValue = await titleField.InputValueAsync();
        actualValue.Length.ShouldBe(15);
        actualValue.ShouldBe("123456789012345");
    }

    [Test]
    public async Task UpdateWorkOrder_WithTitleAt15Characters_Succeeds()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "UpdatedTitle123");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("UpdatedTitle123");
    }

    [Test]
    public async Task UpdateWorkOrder_ChangingFromValidToOver15Characters_PreventedByUI()
    {
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await titleField.FillAsync("12345678901234567890");

        var actualValue = await titleField.InputValueAsync();
        actualValue.Length.ShouldBe(15);
        actualValue.ShouldBe("123456789012345");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(titleField).ToHaveValueAsync("123456789012345");
    }
}
