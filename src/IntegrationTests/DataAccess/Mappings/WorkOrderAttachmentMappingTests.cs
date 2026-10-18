using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

public class WorkOrderAttachmentMappingTests
{
    [Test]
    public void ShouldPersistContentTypeUpTo255Characters()
    {
        new DatabaseTests().Clean();

        var uploader = new Employee("attach255", "First", "Last", "attach255@example.com");
        var workOrder = new WorkOrder { Number = "WO-255", Creator = uploader };
        var longContentType = new string('C', 255);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workOrder);
            context.SaveChanges();
            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = workOrder.Id,
                FileName = "file.bin",
                ContentType = longContentType,
                FileSize = 1,
                UploadedById = uploader.Id,
                UploadedDate = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<WorkOrderAttachment>().Single(a => a.WorkOrderId == workOrder.Id);
            rehydrated.ContentType.ShouldBe(longContentType);
            rehydrated.ContentType.Length.ShouldBe(255);
        }
    }
}
