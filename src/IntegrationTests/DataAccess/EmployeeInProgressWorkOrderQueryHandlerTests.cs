using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class EmployeeInProgressWorkOrderQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnInProgressWorkOrder_WhenEmployeeHasOne()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("testuser", "Test", "User", "test@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO001",
            Title = "In Progress Work",
            Status = WorkOrderStatus.InProgress,
            Assignee = employee,
            Creator = employee
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
        result.Number.ShouldBe("WO001");
        result.Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public async Task ShouldReturnNull_WhenEmployeeHasNoInProgressWorkOrder()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("testuser", "Test", "User", "test@example.com");

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
    public async Task ShouldReturnNull_WhenEmployeeHasCompletedWorkOrder()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("testuser", "Test", "User", "test@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO001",
            Title = "Completed Work",
            Status = WorkOrderStatus.Complete,
            Assignee = employee,
            Creator = employee
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

        result.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnNull_WhenEmployeeHasCancelledWorkOrder()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("testuser", "Test", "User", "test@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO001",
            Title = "Cancelled Work",
            Status = WorkOrderStatus.Cancelled,
            Assignee = employee,
            Creator = employee
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

        result.ShouldBeNull();
    }
}
