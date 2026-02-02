using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderValidationTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldShowRequiredFieldIndicators()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify that the Title and Description labels have required indicators
        var titleLabel = Page.Locator("label[for='Title']");
        await Expect(titleLabel).ToContainTextAsync("Title: *");

        var descriptionLabel = Page.Locator("label[for='Description']");
        await Expect(descriptionLabel).ToContainTextAsync("Description: *");

        await TakeScreenshotAsync(1, "RequiredFieldIndicators");
    }

    [Test]
    public async Task ShouldShowValidationErrorWhenTitleIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill only Description, leave Title empty
        await Input(nameof(WorkOrderManage.Elements.Description), "Test Description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + "Save";
        await Click(saveButtonTestId);

        // Wait for validation message to appear
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Check for validation summary or error message
        var validationMessage = Page.Locator("text=/Title.*required|The Title field is required/i").First;
        await Expect(validationMessage).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });

        await TakeScreenshotAsync(2, "TitleValidationError");
    }

    [Test]
    public async Task ShouldShowValidationErrorWhenDescriptionIsEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill only Title, leave Description empty
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Title");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + "Save";
        await Click(saveButtonTestId);

        // Wait for validation message to appear
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Check for validation summary or error message
        var validationMessage = Page.Locator("text=/Description.*required|The Description field is required/i").First;
        await Expect(validationMessage).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });

        await TakeScreenshotAsync(2, "DescriptionValidationError");
    }

    [Test]
    public async Task ShouldShowValidationErrorWhenBothFieldsAreEmpty()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Leave both Title and Description empty, only fill Room
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + "Save";
        await Click(saveButtonTestId);

        // Wait for validation message to appear
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Check for validation summary containing both errors
        var validationSummary = Page.Locator("ul.validation-errors, .validation-summary-errors");
        await Expect(validationSummary).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });

        await TakeScreenshotAsync(2, "BothFieldsValidationError");
    }
}
