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
public class WorkOrderAttachmentHandlerTests : IntegratedTestBase
{
    [Test]
    public async Task AddAttachmentMetadataCommand_ShouldPersistAttachment()
    {
        new DatabaseTests().Clean();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder { Number = "WO-001", Creator = uploader };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workOrder);
            context.SaveChanges();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new AddAttachmentMetadataCommand(workOrder, uploader, "damage-photo.jpg", "image/jpeg", 4096);
        var attachment = await bus.Send(command);

        attachment.ShouldNotBeNull();
        attachment.FileName.ShouldBe("damage-photo.jpg");
        attachment.ContentType.ShouldBe("image/jpeg");
        attachment.FileSize.ShouldBe(4096L);
        attachment.WorkOrderId.ShouldBe(workOrder.Id);
        attachment.UploadedById.ShouldBe(uploader.Id);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<WorkOrderAttachment>().SingleOrDefault(a => a.Id == attachment.Id);
            persisted.ShouldNotBeNull();
            persisted!.FileName.ShouldBe("damage-photo.jpg");
        }
    }

    [Test]
    public async Task WorkOrderAttachmentsQuery_ShouldReturnAttachmentsForWorkOrder()
    {
        new DatabaseTests().Clean();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder { Number = "WO-002", Creator = uploader };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workOrder);
            context.SaveChanges();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        await bus.Send(new AddAttachmentMetadataCommand(workOrder, uploader, "photo1.jpg", "image/jpeg", 1024));
        await bus.Send(new AddAttachmentMetadataCommand(workOrder, uploader, "invoice.pdf", "application/pdf", 2048));

        var attachments = await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));

        attachments.Length.ShouldBe(2);
        attachments.Any(a => a.FileName == "photo1.jpg").ShouldBeTrue();
        attachments.Any(a => a.FileName == "invoice.pdf").ShouldBeTrue();
    }

    [Test]
    public async Task WorkOrderAttachmentsQuery_ShouldReturnEmptyForWorkOrderWithNoAttachments()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        var workOrder = new WorkOrder { Number = "WO-003", Creator = creator };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var attachments = await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));

        attachments.ShouldBeEmpty();
    }
}
