using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderAttachmentsQueryHandlerTests
{
    [Test]
    public async Task ShouldNotReturnAttachments_WhenTheyBelongToAnotherWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("woatt_iso", "First", "Last", "email@test.com");
        Guid orderAId;
        Guid orderBId;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            var orderA = new WorkOrder { Creator = creator, Number = "WOATT-A", Status = WorkOrderStatus.Draft };
            var orderB = new WorkOrder { Creator = creator, Number = "WOATT-B", Status = WorkOrderStatus.Draft };
            context.Add(orderA);
            context.Add(orderB);
            await context.SaveChangesAsync();
            orderAId = orderA.Id;
            orderBId = orderB.Id;

            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = orderAId,
                FileName = "a.pdf",
                ContentType = "application/pdf",
                FileSize = 100,
                UploadedById = creator.Id,
                UploadedDate = DateTime.UtcNow
            });
            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = orderBId,
                FileName = "b.pdf",
                ContentType = "application/pdf",
                FileSize = 200,
                UploadedById = creator.Id,
                UploadedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderAttachmentsQueryHandler(dataContext);
        var result = await handler.Handle(new WorkOrderAttachmentsQuery(orderAId));

        result.Length.ShouldBe(1);
        result[0].FileName.ShouldBe("a.pdf");
        result.ShouldNotContain(a => a.WorkOrderId == orderBId);
    }

    [Test]
    public async Task ShouldReturnAllAttachments_WhenMultipleExistOnOneWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("woatt_mul", "First", "Last", "email@test.com");
        Guid orderId;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            var order = new WorkOrder { Creator = creator, Number = "WOATT-M", Status = WorkOrderStatus.Draft };
            context.Add(order);
            await context.SaveChangesAsync();
            orderId = order.Id;

            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = orderId,
                FileName = "one.txt",
                ContentType = "text/plain",
                FileSize = 11,
                UploadedById = creator.Id,
                UploadedDate = DateTime.UtcNow.AddMinutes(-2)
            });
            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = orderId,
                FileName = "two.png",
                ContentType = "image/png",
                FileSize = 22,
                UploadedById = creator.Id,
                UploadedDate = DateTime.UtcNow.AddMinutes(-1)
            });
            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = orderId,
                FileName = "three.jpg",
                ContentType = "image/jpeg",
                FileSize = 33,
                UploadedById = creator.Id,
                UploadedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderAttachmentsQueryHandler(dataContext);
        var result = await handler.Handle(new WorkOrderAttachmentsQuery(orderId));

        result.Length.ShouldBe(3);
        result.ShouldContain(a => a.FileName == "one.txt" && a.ContentType == "text/plain" && a.FileSize == 11);
        result.ShouldContain(a => a.FileName == "two.png" && a.ContentType == "image/png" && a.FileSize == 22);
        result.ShouldContain(a => a.FileName == "three.jpg" && a.ContentType == "image/jpeg" && a.FileSize == 33);
    }

    [Test]
    public async Task ShouldDeleteAttachments_WhenWorkOrderIsDeleted()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("woatt_cas", "First", "Last", "email@test.com");
        Guid orderId;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            var order = new WorkOrder { Creator = creator, Number = "WOATT-C", Status = WorkOrderStatus.Draft };
            context.Add(order);
            await context.SaveChangesAsync();
            orderId = order.Id;

            context.Add(new WorkOrderAttachment
            {
                WorkOrderId = orderId,
                FileName = "cascade.pdf",
                ContentType = "application/pdf",
                FileSize = 50,
                UploadedById = creator.Id,
                UploadedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var order = await context.Set<WorkOrder>().SingleAsync(wo => wo.Id == orderId);
            context.Remove(order);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var remaining = await context.Set<WorkOrderAttachment>()
                .Where(a => a.WorkOrderId == orderId)
                .ToListAsync();
            remaining.ShouldBeEmpty();
        }
    }
}
