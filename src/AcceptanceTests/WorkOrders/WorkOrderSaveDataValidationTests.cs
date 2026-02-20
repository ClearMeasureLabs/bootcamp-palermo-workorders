using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDataValidationTests : AcceptanceTestBase
{
[Test, Retry(2)]
public async Task ShouldShowValidationErrorForEmptyTitle()
{
await LoginAsCurrentUser();

await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
await Page.WaitForURLAsync("**/workorder/manage?mode=New");

await Input(nameof(WorkOrderManage.Elements.Title), "");
await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

var validationMessage = Page.Locator(".validation-message:has-text('Title is required')");
await Expect(validationMessage).ToBeVisibleAsync();
}

[Test, Retry(2)]
public async Task ShouldShowValidationErrorForEmptyDescription()
{
await LoginAsCurrentUser();

await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
await Page.WaitForURLAsync("**/workorder/manage?mode=New");

await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
await Input(nameof(WorkOrderManage.Elements.Description), "");
await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

var validationMessage = Page.Locator(".validation-message:has-text('Description is required')");
await Expect(validationMessage).ToBeVisibleAsync();
}

[Test, Retry(2)]
public async Task ShouldShowValidationErrorsForBothEmptyFields()
{
await LoginAsCurrentUser();

await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
await Page.WaitForURLAsync("**/workorder/manage?mode=New");

await Input(nameof(WorkOrderManage.Elements.Title), "");
await Input(nameof(WorkOrderManage.Elements.Description), "");
await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

var titleValidationMessage = Page.Locator(".validation-message:has-text('Title is required')");
await Expect(titleValidationMessage).ToBeVisibleAsync();

var descriptionValidationMessage = Page.Locator(".validation-message:has-text('Description is required')");
await Expect(descriptionValidationMessage).ToBeVisibleAsync();
}

[Test, Retry(2)]
public async Task ShouldClearValidationErrorsWhenFieldsAreCorrected()
{
await LoginAsCurrentUser();

await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
await Page.WaitForURLAsync("**/workorder/manage?mode=New");

await Input(nameof(WorkOrderManage.Elements.Title), "");
await Input(nameof(WorkOrderManage.Elements.Description), "");
await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

var titleValidationMessage = Page.Locator(".validation-message:has-text('Title is required')");
await Expect(titleValidationMessage).ToBeVisibleAsync();

await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");

await Expect(titleValidationMessage).Not.ToBeVisibleAsync();
}

[Test, Retry(2)]
public async Task ShouldDisplayServerValidationErrorInAlert()
{
await LoginAsCurrentUser();

await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
await Page.WaitForURLAsync("**/workorder/manage?mode=New");

await Input(nameof(WorkOrderManage.Elements.Title), "Valid title");
await Input(nameof(WorkOrderManage.Elements.Description), "Valid description");
await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

await Page.EvaluateAsync(@"
const originalFetch = window.fetch;
window.fetch = function(...args) {
return originalFetch(...args).then(response => {
if (response.ok) {
return new Response(JSON.stringify({ error: 'Title is required' }), {
status: 400,
statusText: 'Bad Request',
headers: response.headers
});
}
return response;
});
};
");

await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

var errorAlert = Page.Locator(".alert-danger");
await Expect(errorAlert).ToBeVisibleAsync(new() { Timeout = 5000 });
}
}
