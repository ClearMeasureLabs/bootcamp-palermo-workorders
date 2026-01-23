using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderRequiredFieldsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldDisplayRequiredIndicatorsOnNewWorkOrderForm()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var titleLabel = Page.Locator("label[for='Title']");
        await Expect(titleLabel).ToContainTextAsync("*");

        var descriptionLabel = Page.Locator("label[for='Description']");
        await Expect(descriptionLabel).ToContainTextAsync("*");
    }

    [Test]
    public async Task ShouldDisableSaveButtonWhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(saveButton).ToBeDisabledAsync();
    }

    [Test]
    public async Task ShouldDisableSaveButtonWhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "");

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(saveButton).ToBeDisabledAsync();
    }

    [Test]
    public async Task ShouldDisableSaveButtonWhenBothTitleAndDescriptionAreEmpty()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "");

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(saveButton).ToBeDisabledAsync();
    }

    [Test]
    public async Task ShouldEnableSaveButtonWhenBothTitleAndDescriptionAreProvided()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(saveButton).ToBeEnabledAsync();
    }

    [Test]
    public async Task ShouldDisplayRequiredIndicatorsOnEditWorkOrderForm()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleLabel = Page.Locator("label[for='Title']");
        await Expect(titleLabel).ToContainTextAsync("*");

        var descriptionLabel = Page.Locator("label[for='Description']");
        await Expect(descriptionLabel).ToContainTextAsync("*");
    }

    [Test]
    public async Task ShouldDisableSaveButtonOnEditWhenTitleIsCleared()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Title), "");

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(saveButton).ToBeDisabledAsync();
    }

    [Test]
    public async Task ShouldDisableSaveButtonOnEditWhenDescriptionIsCleared()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Input(nameof(WorkOrderManage.Elements.Description), "");

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Expect(saveButton).ToBeDisabledAsync();
    }
}
