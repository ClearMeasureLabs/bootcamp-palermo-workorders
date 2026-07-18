using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderAttachmentTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAddAttachmentMetadataAndDisplayOnManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        var attachment = new WorkOrderAttachment
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
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

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var attachmentsSection = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentsSection));
        await Expect(attachmentsSection).ToBeVisibleAsync();

        var fileNameCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentFileName));
        await Expect(fileNameCell).ToContainTextAsync("damage-photo.jpg");

        var contentTypeCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentContentType));
        await Expect(contentTypeCell).ToContainTextAsync("image/jpeg");

        var fileSizeCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentFileSize));
        await Expect(fileSizeCell).ToContainTextAsync("2048");

        var uploaderCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentUploadedBy));
        await Expect(uploaderCell).ToContainTextAsync(CurrentUser.GetFullName());
    }
}
