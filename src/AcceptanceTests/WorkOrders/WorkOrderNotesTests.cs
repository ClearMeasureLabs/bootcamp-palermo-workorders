using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderNotesTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAddNoteAndDisplayAuthorTimestampAndText()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        const string noteText = "This is a test note for the work order.";
        await Input(nameof(WorkOrderManage.Elements.NoteTextInput), noteText);
        await Click(nameof(WorkOrderManage.Elements.AddNoteButton));

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var noteRow = Page.GetByTestId(nameof(WorkOrderManage.Elements.NoteRow)).First;
        await Expect(noteRow).ToBeVisibleAsync();

        var noteAuthor = Page.GetByTestId(nameof(WorkOrderManage.Elements.NoteAuthor)).First;
        await Expect(noteAuthor).ToContainTextAsync(CurrentUser.GetFullName());

        var noteTimestamp = Page.GetByTestId(nameof(WorkOrderManage.Elements.NoteTimestamp)).First;
        var timestampText = await noteTimestamp.InnerTextAsync();
        timestampText.ShouldNotBeNullOrEmpty();

        var noteTextLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.NoteText)).First;
        await Expect(noteTextLocator).ToContainTextAsync(noteText);
    }

    [Test, Retry(2)]
    public async Task ShouldShowValidationMessage_WhenNoteTextIsEmpty()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);

        await Click(nameof(WorkOrderManage.Elements.AddNoteButton));

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var validationMessage = Page.GetByTestId(nameof(WorkOrderManage.Elements.NoteValidationMessage));
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("Note text is required");
    }
}
