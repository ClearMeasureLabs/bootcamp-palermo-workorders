using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestAttachmentTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAddAttachmentMetadataAndDisplayOnManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();

        var attachment = new WorkRequestAttachment
        {
            Id = Guid.NewGuid(),
            WorkRequestId = order.Id,
            FileName = "damage-photo.jpg",
            ContentType = "image/jpeg",
            FileSize = 2048,
            UploadedById = CurrentUser.Id,
            UploadedBy = CurrentUser,
            UploadedDate = DateTime.UtcNow
        };

        var command = new AddAttachmentMetadataCommand(
            order, CurrentUser, attachment.FileName, attachment.ContentType, attachment.FileSize);
        await Bus.Send(command);

        await Click(nameof(WorkRequestSearch.Elements.WorkRequestLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkRequestManage.Elements.WorkRequestNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var attachmentsSection = Page.GetByTestId(nameof(WorkRequestManage.Elements.AttachmentsSection));
        await Expect(attachmentsSection).ToBeVisibleAsync();

        var fileNameCell = Page.GetByTestId(nameof(WorkRequestManage.Elements.AttachmentFileName));
        await Expect(fileNameCell).ToContainTextAsync("damage-photo.jpg");

        var contentTypeCell = Page.GetByTestId(nameof(WorkRequestManage.Elements.AttachmentContentType));
        await Expect(contentTypeCell).ToContainTextAsync("image/jpeg");

        var fileSizeCell = Page.GetByTestId(nameof(WorkRequestManage.Elements.AttachmentFileSize));
        await Expect(fileSizeCell).ToContainTextAsync("2048");

        var uploaderCell = Page.GetByTestId(nameof(WorkRequestManage.Elements.AttachmentUploadedBy));
        await Expect(uploaderCell).ToContainTextAsync(CurrentUser.GetFullName());
    }
}
