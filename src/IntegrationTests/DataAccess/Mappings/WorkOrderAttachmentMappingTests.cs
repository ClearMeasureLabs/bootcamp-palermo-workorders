using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderAttachmentMappingTests
{
    [Test]
    public void ShouldPersistDescription()
    {
        new DatabaseTests().Clean();

        var uploader = new Employee("userattach", "First", "Last", "emailattach@example.com");
        var workOrder = new WorkOrder { Creator = uploader, Number = "WO-ATT", Title = "Attachment test" };
        var attachment = new WorkOrderAttachment
        {
            WorkOrderId = workOrder.Id,
            FileName = "photo.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            Description = "Front door damage",
            UploadedById = uploader.Id,
            UploadedDate = DateTime.UtcNow
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workOrder);
            context.SaveChanges();
            attachment.WorkOrderId = workOrder.Id;
            attachment.UploadedById = uploader.Id;
            context.Add(attachment);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<WorkOrderAttachment>().Single(a => a.Id == attachment.Id);
            rehydrated.Description.ShouldBe("Front door damage");
        }
    }
}
