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
        var uploadDate = new DateTime(2025, 3, 1, 8, 0, 0);
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
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

    [Test]
    public void CreateAttachment_WhenFileSizeExceedsTenMb_ShouldThrowArgumentException()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var command = new AddAttachmentMetadataCommand(
            workOrder, uploader, "large-photo.jpg", "image/jpeg", 10 * 1024 * 1024 + 1);

        Should.Throw<ArgumentException>(() => command.CreateAttachment(DateTime.UtcNow))
            .ParamName.ShouldBe(nameof(AddAttachmentMetadataCommand.FileSize));
    }

    [TestCase("malware.exe")]
    [TestCase("script.bat")]
    [TestCase("run.cmd")]
    [TestCase("hack.ps1")]
    [TestCase("Hack.EXE")]
    public void CreateAttachment_WhenExtensionIsBlocked_ShouldThrowArgumentException(string fileName)
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var command = new AddAttachmentMetadataCommand(
            workOrder, uploader, fileName, "application/octet-stream", 1024);

        Should.Throw<ArgumentException>(() => command.CreateAttachment(DateTime.UtcNow))
            .ParamName.ShouldBe(nameof(AddAttachmentMetadataCommand.FileName));
    }

    [Test]
    public void CreateAttachment_WhenFileSizeIsExactlyTenMb_ShouldSucceed()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var command = new AddAttachmentMetadataCommand(
            workOrder, uploader, "boundary.jpg", "image/jpeg", 10 * 1024 * 1024);

        var attachment = command.CreateAttachment(DateTime.UtcNow);

        attachment.FileSize.ShouldBe(10 * 1024 * 1024L);
    }
}
