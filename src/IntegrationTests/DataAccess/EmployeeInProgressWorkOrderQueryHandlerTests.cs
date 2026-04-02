using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class EmployeeInProgressWorkOrderQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnInProgressWorkOrderWhenEmployeeHasOne()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("emp1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO001",
            Title = "Test Work Order",
            Creator = employee,
            Assignee = employee,
            Status = WorkOrderStatus.InProgress
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(workOrder);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeInProgressWorkOrderQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeInProgressWorkOrderQuery(employee));

        result.ShouldNotBeNull();
        result.Id.ShouldBe(workOrder.Id);
        result.Number.ShouldBe("WO001");
    }

    [Test]
    public async Task ShouldReturnNullWhenEmployeeHasNoInProgressWorkOrder()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("emp1", "John", "Doe", "john@example.com");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeInProgressWorkOrderQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeInProgressWorkOrderQuery(employee));

        result.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnNullWhenEmployeeHasCompletedWorkOrder()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("emp1", "John", "Doe", "john@example.com");
        var creator = new Employee("creator1", "Creator", "Test", "creator@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO001",
            Title = "Test Work Order",
            Creator = creator,
            Assignee = employee,
            Status = WorkOrderStatus.Complete
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeInProgressWorkOrderQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeInProgressWorkOrderQuery(employee));

        result.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnNullWhenEmployeeHasAssignedWorkOrder()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("emp1", "John", "Doe", "john@example.com");
        var creator = new Employee("creator1", "Creator", "Test", "creator@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO001",
            Title = "Test Work Order",
            Creator = creator,
            Assignee = employee,
            Status = WorkOrderStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeInProgressWorkOrderQueryHandler(dataContext);
        var result = await handler.Handle(new EmployeeInProgressWorkOrderQuery(employee));

        result.ShouldBeNull();
    }
}
