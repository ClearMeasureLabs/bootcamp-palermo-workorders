using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpWorkOrderToolTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldListAllWorkOrders()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order1 = new WorkOrder { Creator = employee, Number = "WO-001", Title = "Fix sink" };
        var order2 = new WorkOrder { Creator = employee, Number = "WO-002", Title = "Paint wall" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus);

        result.ShouldContain("WO-001");
        result.ShouldContain("WO-002");
        result.ShouldContain("Fix sink");
        result.ShouldContain("Paint wall");
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByStatus()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var draftOrder = new WorkOrder { Creator = employee, Number = "WO-001", Title = "Draft order", Status = WorkOrderStatus.Draft };
        var assignedOrder = new WorkOrder { Creator = employee, Number = "WO-002", Title = "Assigned order", Status = WorkOrderStatus.Assigned };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(draftOrder);
            context.Add(assignedOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus, "Assigned");

        result.ShouldContain("WO-002");
        result.ShouldNotContain("WO-001");
    }

    [Test]
    public async Task ShouldGetWorkOrderByNumber()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = "WO-100", Title = "Test order", Description = "A description", RoomNumber = "101" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, "WO-100");

        result.ShouldContain("WO-100");
        result.ShouldContain("Test order");
        result.ShouldContain("A description");
        result.ShouldContain("101");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingWorkOrder()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, "NONEXISTENT");

        result.ShouldContain("No work order found");
    }

    [Test]
    public async Task ShouldCreateDraftWorkOrder()
    {
        var employee = new Employee("creator1", "Jane", "Smith", "jane@test.com");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.CreateWorkOrder(bus, "New Work Order", "Fix the broken window", "creator1");

        result.ShouldContain("New Work Order");
        result.ShouldContain("Fix the broken window");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldReturnErrorForMissingCreator()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.CreateWorkOrder(bus, "Title", "Description", "nonexistent_user");

        result.ShouldContain("not found");
    }

    [Test]
    public async Task ShouldUpdateWorkOrderDescription()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = "WO-200", Title = "Original", Description = "Old description" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrderDescription(bus, "WO-200", "Updated description", "user1");

        result.ShouldContain("Updated description");
    }

    [Test]
    public async Task ShouldReturnErrorForDescriptionUpdateOnMissingWorkOrder()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrderDescription(bus, "MISSING", "New desc", "user1");

        result.ShouldContain("No work order found");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownCommand()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = "WO-300", Title = "Test" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-300", "FakeCommand", "user1");

        result.ShouldContain("Unknown command");
        result.ShouldContain("Available commands");
    }
}
