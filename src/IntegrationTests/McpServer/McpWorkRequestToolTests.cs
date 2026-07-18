using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpWorkRequestToolTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldListAllWorkRequests()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order1 = new WorkRequest { Creator = employee, Number = "WO-001", Title = "Fix sink" };
        var order2 = new WorkRequest { Creator = employee, Number = "WO-002", Title = "Paint wall" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ListWorkRequests(bus);

        result.ShouldContain("WO-001");
        result.ShouldContain("WO-002");
        result.ShouldContain("Fix sink");
        result.ShouldContain("Paint wall");
    }

    [Test]
    public async Task ShouldFilterWorkRequestsByStatus()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var draftOrder = new WorkRequest { Creator = employee, Number = "WO-001", Title = "Draft order", Status = WorkRequestStatus.Draft };
        var assignedOrder = new WorkRequest { Creator = employee, Number = "WO-002", Title = "Assigned order", Status = WorkRequestStatus.Assigned };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(draftOrder);
            context.Add(assignedOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ListWorkRequests(bus, "Assigned");

        result.ShouldContain("WO-002");
        result.ShouldNotContain("WO-001");
    }

    [Test]
    public async Task ShouldGetWorkRequestByNumber()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkRequest { Creator = employee, Number = "WO-100", Title = "Test order", Description = "A description", RoomNumber = "101" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.GetWorkRequest(bus, "WO-100");

        result.ShouldContain("WO-100");
        result.ShouldContain("Test order");
        result.ShouldContain("A description");
        result.ShouldContain("101");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingWorkRequest()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.GetWorkRequest(bus, "NONEXISTENT");

        result.ShouldContain("No work request found");
    }

    [Test]
    public async Task ShouldCreateDraftWorkRequest()
    {
        var employee = new Employee("creator1", "Jane", "Smith", "jane@test.com");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkRequestNumberGenerator();
        var result = await WorkRequestTools.CreateWorkRequest(bus, numberGenerator, "New Work Request", "Fix the broken window", "creator1");

        result.ShouldContain("New Work Request");
        result.ShouldContain("Fix the broken window");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldReturnErrorForMissingCreator()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkRequestNumberGenerator();
        var result = await WorkRequestTools.CreateWorkRequest(bus, numberGenerator, "Title", "Description", "nonexistent_user");

        result.ShouldContain("not found");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownCommand()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkRequest { Creator = employee, Number = "WO-300", Title = "Test" };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ExecuteWorkRequestCommand(bus, "WO-300", "FakeCommand", "user1");

        result.ShouldContain("Unknown command");
        result.ShouldContain("Available commands");
    }

    [Test]
    public async Task ShouldExecuteCancelCommand()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        //var order = new WorkRequest { Creator = employee, Number = "WO-300", Title = "Test" };
        var assignedOrder = new WorkRequest { Creator = employee, Number = "WO-002", Title = "Assigned order", Status = WorkRequestStatus.Assigned };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(assignedOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ExecuteWorkRequestCommand(bus, "WO-002", "AssignedToCancelledCommand", "user1");

        WorkRequest? wo = null;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = context.Set<WorkRequest>().Where(wo => wo.Number == "WO-002").Single();
        }

        wo.Status.ShouldBe(WorkRequestStatus.Cancelled);
        result.ShouldContain("Cancelled");
    }

    [Test]
    public async Task ShouldExecuteShelveCommand()
    {
        var creator = new Employee("creator1", "Timothy", "Lovejoy", "timothy@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "willie@test.com");
        var inProgressOrder = new WorkRequest
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-778",
            Title = "Mow grass",
            Status = WorkRequestStatus.InProgress
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(inProgressOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ExecuteWorkRequestCommand(bus, "WO-778", "InProgressToAssignedCommand", "gwillie");

        WorkRequest? wo = null;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = context.Set<WorkRequest>().Single(wo => wo.Number == "WO-778");
        }

        wo.Status.ShouldBe(WorkRequestStatus.Assigned);
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldReturnErrorWhenDraftToAssignedMissingAssignee()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role("Manager", true, false));
        var draftOrder = new WorkRequest
        {
            Creator = creator,
            Number = "WO-400",
            Title = "Needs assignee",
            Status = WorkRequestStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ExecuteWorkRequestCommand(bus, "WO-400", "DraftToAssignedCommand", "creator1");

        result.ShouldContain("requires an assigneeUsername");
    }

    [Test]
    public async Task ShouldExecuteShelveAliasCommand()
    {
        var creator = new Employee("creator1", "Timothy", "Lovejoy", "timothy@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "willie@test.com");
        assignee.AddRole(new Role("Worker", false, true));
        var inProgressOrder = new WorkRequest
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-779",
            Title = "Shelve alias",
            Status = WorkRequestStatus.InProgress
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(inProgressOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkRequestTools.ExecuteWorkRequestCommand(bus, "WO-779", "Shelve", "gwillie");

        WorkRequest? wo;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = await context.Set<WorkRequest>().SingleAsync(w => w.Number == "WO-779");
        }

        wo.Status.ShouldBe(WorkRequestStatus.Assigned);
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldExecuteDraftToAssignedThenBeginStatusChanges()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role("Manager", true, false));
        var assignee = new Employee("worker1", "Sam", "Worker", "worker@test.com");
        assignee.AddRole(new Role("Worker", false, true));
        var draftOrder = new WorkRequest
        {
            Creator = creator,
            Number = "WO-402",
            Title = "Status flow",
            Status = WorkRequestStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var assignResult = await WorkRequestTools.ExecuteWorkRequestCommand(
            bus,
            "WO-402",
            "DraftToAssignedCommand",
            "creator1",
            "worker1");

        assignResult.ShouldContain("Assigned");

        var beginResult = await WorkRequestTools.ExecuteWorkRequestCommand(
            bus,
            "WO-402",
            "AssignedToInProgressCommand",
            "worker1");

        beginResult.ShouldContain("In Progress");

        WorkRequest? wo;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = await context.Set<WorkRequest>().SingleAsync(w => w.Number == "WO-402");
        }

        wo.Status.ShouldBe(WorkRequestStatus.InProgress);
        wo.Assignee!.UserName.ShouldBe("worker1");
    }
}
