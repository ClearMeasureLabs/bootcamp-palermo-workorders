using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class OpenWorkOrderCountByAssigneeQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnOpenCountsPerAssigneeFromDatabase()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("owoca_c", "First", "Last", "email@test.com");
        var assigneeA = new Employee("owoca_a", "Alice", "Assignee", "a@test.com");
        var assigneeB = new Employee("owoca_b", "Bob", "Assignee", "b@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assigneeA);
            context.Add(assigneeB);
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = assigneeA,
                Number = "OA-1",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = assigneeA,
                Number = "OA-2",
                Status = WorkOrderStatus.InProgress
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = assigneeB,
                Number = "OA-3",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = assigneeB,
                Number = "OA-4",
                Status = WorkOrderStatus.Draft
            });
            context.Add(new WorkOrder
            {
                Creator = creator,
                Assignee = assigneeB,
                Number = "OA-5",
                Status = WorkOrderStatus.Complete
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new OpenWorkOrderCountByAssigneeQueryHandler(dataContext);
        var result = await handler.Handle(new OpenWorkOrderCountByAssigneeQuery());

        result[assigneeA.UserName].ShouldBe(2);
        result[assigneeB.UserName].ShouldBe(1);
        result.ContainsKey(creator.UserName).ShouldBeFalse();
    }
}
