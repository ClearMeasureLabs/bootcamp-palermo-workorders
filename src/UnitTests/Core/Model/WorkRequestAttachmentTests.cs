using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkRequestAttachmentTests
{
    [Test]
    public void WorkRequestAttachment_ShouldRequireFileName()
    {
        var workRequest = new WorkRequest { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        uploader.Id = Guid.NewGuid();

        var command = new ClearMeasure.Bootcamp.Core.Model.StateCommands.AddAttachmentMetadataCommand(
            workRequest, uploader, "", "image/jpeg", 1024);

        Assert.Throws<ArgumentException>(() => command.CreateAttachment(DateTime.UtcNow));
    }

    [Test]
    public void WorkRequestAttachment_ShouldSetUploadedDate()
    {
        var workRequest = new WorkRequest { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        uploader.Id = Guid.NewGuid();
        var expectedDate = new DateTime(2025, 1, 15, 10, 0, 0);

        var command = new ClearMeasure.Bootcamp.Core.Model.StateCommands.AddAttachmentMetadataCommand(
            workRequest, uploader, "damage-photo.jpg", "image/jpeg", 2048);

        var attachment = command.CreateAttachment(expectedDate);

        attachment.UploadedDate.ShouldBe(expectedDate);
    }
}
