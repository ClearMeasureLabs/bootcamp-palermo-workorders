using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class EmployeeWorkloadQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnCountsByStatus_ForTargetEmployeeOnly()
    {
        new DatabaseTests().Clean();

        var hank = new Employee("hank", "Hank", "Hill", "hank@test.com");
        var other = new Employee("other", "Other", "Person", "other@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(hank);
            context.Add(other);

            context.Add(new WorkOrder
            {
                Creator = hank,
                Assignee = hank,
                Number = "WL-1",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = hank,
                Assignee = hank,
                Number = "WL-2",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = hank,
                Assignee = hank,
                Number = "WL-3",
                Status = WorkOrderStatus.InProgress
            });
            context.Add(new WorkOrder
            {
                Creator = hank,
                Assignee = hank,
                Number = "WL-4",
                Status = WorkOrderStatus.Draft
            });
            context.Add(new WorkOrder
            {
                Creator = other,
                Assignee = other,
                Number = "WL-5",
                Status = WorkOrderStatus.Assigned
            });
            context.Add(new WorkOrder
            {
                Creator = other,
                Assignee = other,
                Number = "WL-6",
                Status = WorkOrderStatus.InProgress
            });

            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeWorkloadQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeWorkloadQuery("hank"));

        result.ShouldNotBeNull();
        result!.Username.ShouldBe("hank");
        result.AssignedCount.ShouldBe(2);
        result.InProgressCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldReturnNull_WhenEmployeeDoesNotExist()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeWorkloadQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeWorkloadQuery("missing"));

        result.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnZeroCounts_WhenEmployeeHasNoActiveAssignments()
    {
        new DatabaseTests().Clean();

        var hank = new Employee("hank", "Hank", "Hill", "hank@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(hank);
            context.Add(new WorkOrder
            {
                Creator = hank,
                Assignee = hank,
                Number = "WL-DONE",
                Status = WorkOrderStatus.Complete
            });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeWorkloadQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeWorkloadQuery("hank"));

        result.ShouldNotBeNull();
        result!.AssignedCount.ShouldBe(0);
        result.InProgressCount.ShouldBe(0);
    }
}
