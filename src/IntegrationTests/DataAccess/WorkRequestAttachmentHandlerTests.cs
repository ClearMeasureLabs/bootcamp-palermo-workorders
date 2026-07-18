using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkRequestAttachmentHandlerTests : IntegratedTestBase
{
    [Test]
    public async Task AddAttachmentMetadataCommand_ShouldPersistAttachment()
    {
        new DatabaseTests().Clean();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workRequest = new WorkRequest { Number = "WO-001", Creator = uploader };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workRequest);
            context.SaveChanges();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new AddAttachmentMetadataCommand(workRequest, uploader, "damage-photo.jpg", "image/jpeg", 4096);
        var attachment = await bus.Send(command);

        attachment.ShouldNotBeNull();
        attachment.FileName.ShouldBe("damage-photo.jpg");
        attachment.ContentType.ShouldBe("image/jpeg");
        attachment.FileSize.ShouldBe(4096L);
        attachment.WorkRequestId.ShouldBe(workRequest.Id);
        attachment.UploadedById.ShouldBe(uploader.Id);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<WorkRequestAttachment>().SingleOrDefault(a => a.Id == attachment.Id);
            persisted.ShouldNotBeNull();
            persisted!.FileName.ShouldBe("damage-photo.jpg");
        }
    }

    [Test]
    public async Task WorkRequestAttachmentsQuery_ShouldReturnAttachmentsForWorkRequest()
    {
        new DatabaseTests().Clean();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workRequest = new WorkRequest { Number = "WO-002", Creator = uploader };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workRequest);
            context.SaveChanges();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        await bus.Send(new AddAttachmentMetadataCommand(workRequest, uploader, "photo1.jpg", "image/jpeg", 1024));
        await bus.Send(new AddAttachmentMetadataCommand(workRequest, uploader, "invoice.pdf", "application/pdf", 2048));

        var attachments = await bus.Send(new WorkRequestAttachmentsQuery(workRequest.Id));

        attachments.Length.ShouldBe(2);
        attachments.Any(a => a.FileName == "photo1.jpg").ShouldBeTrue();
        attachments.Any(a => a.FileName == "invoice.pdf").ShouldBeTrue();
    }

    [Test]
    public async Task WorkRequestAttachmentsQuery_ShouldReturnEmptyForWorkRequestWithNoAttachments()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workRequest = new WorkRequest { Number = "WO-003", Creator = creator };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workRequest);
            context.SaveChanges();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var attachments = await bus.Send(new WorkRequestAttachmentsQuery(workRequest.Id));

        attachments.ShouldBeEmpty();
    }
}
