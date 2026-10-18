using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderCountForStatusQueryHandlerTests
{
    [Test]
    public async Task ShouldCountWorkOrdersInGivenStatusAcrossTwoStatuses()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("wocfs_user", "First", "Last", "email@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder { Creator = creator, Number = "WOCFS1", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCFS2", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCFS3", Status = WorkOrderStatus.InProgress });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountForStatusQueryHandler(dataContext);

        var draftCount = await handler.Handle(new WorkOrderCountForStatusQuery(WorkOrderStatus.Draft.Key));
        var inProgressCount = await handler.Handle(new WorkOrderCountForStatusQuery(WorkOrderStatus.InProgress.Key));
        var assignedCount = await handler.Handle(new WorkOrderCountForStatusQuery(WorkOrderStatus.Assigned.Key));

        draftCount.ShouldBe(2);
        inProgressCount.ShouldBe(1);
        assignedCount.ShouldBe(0);
    }
}
