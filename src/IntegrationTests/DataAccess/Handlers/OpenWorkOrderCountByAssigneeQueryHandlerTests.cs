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
    public async Task ShouldCountOnlyAssignedAndInProgressForGivenAssignee()
    {
        new DatabaseTests().Clean();

        var assignee = new Employee("owocba_assignee", "First", "Last", "a@test.com");
        var other = new Employee("owocba_other", "Other", "User", "o@test.com");
        var creator = new Employee("owocba_creator", "Creator", "User", "c@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            context.Add(other);
            context.Add(creator);
            context.Add(new WorkOrder
            {
                Creator = creator, Assignee = assignee, Number = "OA-1", Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = creator, Assignee = assignee, Number = "OA-2", Status = WorkOrderStatus.InProgress
            });
            context.Add(new WorkOrder
            {
                Creator = creator, Assignee = assignee, Number = "OA-3", Status = WorkOrderStatus.Draft
            });
            context.Add(new WorkOrder
            {
                Creator = creator, Assignee = assignee, Number = "OA-4", Status = WorkOrderStatus.Complete
            });
            context.Add(new WorkOrder
            {
                Creator = creator, Assignee = other, Number = "OA-5", Status = WorkOrderStatus.Assigned
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new OpenWorkOrderCountByAssigneeQueryHandler(dataContext);
        var result = await handler.Handle(new OpenWorkOrderCountByAssigneeQuery("owocba_assignee"));

        result.ShouldBe(2);
    }
}
