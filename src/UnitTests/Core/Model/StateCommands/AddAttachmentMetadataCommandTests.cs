using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class AddAttachmentMetadataCommandTests
{
    [Test]
    public void AddAttachmentMetadataCommand_ShouldAddAttachment()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        uploader.Id = Guid.NewGuid();
        var uploadDate = new DateTime(2025, 3, 1, 8, 0, 0);

        var command = new AddAttachmentMetadataCommand(
            workOrder, uploader, "damage-photo.jpg", "image/jpeg", 4096);

        var attachment = command.CreateAttachment(uploadDate);

        attachment.FileName.ShouldBe("damage-photo.jpg");
        attachment.ContentType.ShouldBe("image/jpeg");
        attachment.FileSize.ShouldBe(4096L);
        attachment.WorkOrderId.ShouldBe(workOrder.Id);
        attachment.UploadedById.ShouldBe(uploader.Id);
        attachment.UploadedDate.ShouldBe(uploadDate);
    }
}
