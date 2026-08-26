using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services.Impl;
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

        await using (var context = TestHost.GetRequiredService<DbContext>())
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

        await using (var context = TestHost.GetRequiredService<DbContext>())
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
    public async Task ShouldFilterWorkOrdersByCreatorUsername()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(bus, creatorUsername: "tlovejoy");

        ExtractNumbers(result).ShouldBeSet(
            "LJDRAFT",
            "LJWA",
            "LJWIP",
            "LJCOMP");
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByAssigneeUsername()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(bus, assigneeUsername: "gwillie");

        ExtractNumbers(result).ShouldBeSet(
            "LJWA",
            "LJWIP",
            "OTWIP");
    }

    [Test]
    public async Task ShouldAndAssigneeAndStatusFilters()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "InProgress",
            assigneeUsername: "gwillie");

        ExtractNumbers(result).ShouldBeSet(
            "LJWIP",
            "OTWIP");
    }

    [Test]
    public async Task ShouldAndCreatorAndStatusFiltersForLovejoyDrafts()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "Draft",
            creatorUsername: "tlovejoy");

        ExtractNumbers(result).ShouldBeSet("LJDRAFT");
    }

    [Test]
    public async Task ShouldAndCreatorAssigneeAndStatusFilters()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "InProgress",
            creatorUsername: "tlovejoy",
            assigneeUsername: "gwillie");

        ExtractNumbers(result).ShouldBeSet("LJWIP");
    }

    [Test]
    public async Task ShouldTreatOmittedEmptyAndWhitespaceFiltersAsDisabled()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var omitted = ExtractNumbers(await WorkOrderTools.ListWorkOrders(bus));
        var blank = ExtractNumbers(await WorkOrderTools.ListWorkOrders(bus, " ", string.Empty, "   "));

        omitted.Length.ShouldBe(7);
        blank.ShouldBeSet(omitted);
    }

    [Test]
    public async Task ShouldReturnEmptyForUnknownCreatorOrAssigneeWithoutCreatingEmployee()
    {
        await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();
        int employeeCount;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            employeeCount = await context.Set<Employee>().CountAsync();
        }

        var unknownCreator = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "Assigned",
            creatorUsername: "not-a-person");
        var unknownAssignee = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "Draft",
            creatorUsername: "tlovejoy",
            assigneeUsername: "not-a-person");

        unknownCreator.ShouldBe("[]");
        unknownAssignee.ShouldBe("[]");
        await using var verificationContext = TestHost.GetRequiredService<DbContext>();
        (await verificationContext.Set<Employee>().CountAsync()).ShouldBe(employeeCount);
    }

    [Test]
    public async Task ShouldPreserveInvalidStatusFailure()
    {
        var bus = TestHost.GetRequiredService<IBus>();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => WorkOrderTools.ListWorkOrders(bus, status: "NotAStatus"));
    }

    [Test]
    public async Task ShouldGetWorkOrderByNumberWithCompleteDetailShape()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var createdDate = new DateTime(2026, 8, 25, 14, 30, 0, DateTimeKind.Utc);
        var order = new WorkOrder
        {
            Creator = employee,
            Number = "WO-100",
            Title = "Test order",
            Description = "A description",
            Instructions = "preschool quiet",
            RoomNumber = "101",
            DueDate = new DateOnly(2026, 9, 12),
            Status = WorkOrderStatus.Draft,
            CreatedDate = createdDate
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, "WO-100");

        using var document = JsonDocument.Parse(result);
        var detail = document.RootElement;
        detail.GetProperty("Number").GetString().ShouldBe("WO-100");
        detail.GetProperty("Title").GetString().ShouldBe("Test order");
        detail.GetProperty("Description").GetString().ShouldBe("A description");
        detail.GetProperty("Instructions").GetString().ShouldBe("preschool quiet");
        detail.GetProperty("RoomNumber").GetString().ShouldBe("101");
        detail.GetProperty("DueDate").GetString().ShouldBe("2026-09-12");
        detail.GetProperty("Status").GetString().ShouldBe("Draft");
        detail.GetProperty("Creator").GetString().ShouldBe("John Doe");
        detail.GetProperty("CreatorUsername").GetString().ShouldBe("user1");
        detail.GetProperty("Assignee").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("CreatedDate").GetDateTime().ShouldBe(createdDate);
        detail.GetProperty("AssignedDate").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("CompletedDate").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Test]
    public async Task ShouldGetWorkOrderWithEmptyInstructionsPropertyWhenInstructionsOmitted()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder
        {
            Creator = employee,
            Number = "WO-101",
            Title = "No special instructions",
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, "WO-101");

        using var document = JsonDocument.Parse(result);
        var instructions = document.RootElement.GetProperty("Instructions");
        instructions.ValueKind.ShouldBe(JsonValueKind.String);
        instructions.GetString().ShouldBe(string.Empty);
        result.ShouldContain("\"Instructions\": \"\"");
        result.ShouldNotContain("\"Instructions\": \"null\"");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingWorkOrder()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, "NONEXISTENT");

        result.ShouldBe("No work order found with number 'NONEXISTENT'.");
    }

    [Test]
    public async Task ShouldKeepListWorkOrderSummaryWithoutInstructions()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder
        {
            Creator = employee,
            Number = "WO-102",
            Title = "Summary shape",
            Instructions = "detail only"
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus);

        using var document = JsonDocument.Parse(result);
        var summary = document.RootElement.EnumerateArray().Single();
        summary.TryGetProperty("Instructions", out _).ShouldBeFalse();
    }

    [Test]
    public void ShouldKeepWorkOrderToolArgumentListsUnchanged()
    {
        ParameterNames(nameof(WorkOrderTools.CreateWorkOrder)).ShouldBe(
            ["bus", "numberGenerator", "title", "description", "creatorUsername", "roomNumber", "dueDate", "instructions"]);
        ParameterNames(nameof(WorkOrderTools.SaveWorkOrder)).ShouldBe(
            ["bus", "workOrderNumber", "executingUsername", "title", "description", "instructions", "roomNumber", "dueDate"]);
        ParameterNames(nameof(WorkOrderTools.CreateDatedWorkOrders)).ShouldBe(
            ["bus", "timeProvider", "creatorUsername", "assigneeUsername", "title", "description", "dueDates", "saturdayCount"]);
    }

    [Test]
    public async Task ShouldCreateDraftWorkOrder()
    {
        var employee = new Employee("creator1", "Jane", "Smith", "jane@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var result = await WorkOrderTools.CreateWorkOrder(bus, numberGenerator, "New Work Order", "Fix the broken window", "creator1");

        result.ShouldContain("New Work Order");
        result.ShouldContain("Fix the broken window");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldReturnErrorForMissingCreator()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var result = await WorkOrderTools.CreateWorkOrder(bus, numberGenerator, "Title", "Description", "nonexistent_user");

        result.ShouldContain("not found");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownCommand()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = "WO-300", Title = "Test" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
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

    [Test]
    public async Task ShouldExecuteCancelCommand()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        //var order = new WorkOrder { Creator = employee, Number = "WO-300", Title = "Test" };
        var assignedOrder = new WorkOrder { Creator = employee, Number = "WO-002", Title = "Assigned order", Status = WorkOrderStatus.Assigned };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(assignedOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-002", "AssignedToCancelledCommand", "user1");

        WorkOrder wo;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = context.Set<WorkOrder>().Single(wo => wo.Number == "WO-002");
        }

        wo.Status.ShouldBe(WorkOrderStatus.Cancelled);
        result.ShouldContain("Cancelled");
    }

    [Test]
    public async Task ShouldExecuteShelveCommand()
    {
        var creator = new Employee("creator1", "Timothy", "Lovejoy", "timothy@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "willie@test.com");
        var inProgressOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-778",
            Title = "Mow grass",
            Status = WorkOrderStatus.InProgress
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(inProgressOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-778", "InProgressToAssignedCommand", "gwillie");

        WorkOrder wo;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = context.Set<WorkOrder>().Single(wo => wo.Number == "WO-778");
        }

        wo.Status.ShouldBe(WorkOrderStatus.Assigned);
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldReturnErrorWhenDraftToAssignedMissingAssignee()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role("Manager", true, false));
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-400",
            Title = "Needs assignee",
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-400", "DraftToAssignedCommand", "creator1");

        result.ShouldContain("requires an assigneeUsername");
    }

    [Test]
    public async Task ShouldExecuteShelveAliasCommand()
    {
        var creator = new Employee("creator1", "Timothy", "Lovejoy", "timothy@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "willie@test.com");
        assignee.AddRole(new Role("Worker", false, true));
        var inProgressOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-779",
            Title = "Shelve alias",
            Status = WorkOrderStatus.InProgress
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(inProgressOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-779", "Shelve", "gwillie");

        WorkOrder? wo;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = await context.Set<WorkOrder>().SingleAsync(w => w.Number == "WO-779");
        }

        wo.Status.ShouldBe(WorkOrderStatus.Assigned);
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldExecuteDraftToAssignedThenBeginStatusChanges()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role("Manager", true, false));
        var assignee = new Employee("worker1", "Sam", "Worker", "worker@test.com");
        assignee.AddRole(new Role("Worker", false, true));
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-402",
            Title = "Status flow",
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var assignResult = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            "WO-402",
            "DraftToAssignedCommand",
            "creator1",
            "worker1");

        assignResult.ShouldContain("Assigned");

        var beginResult = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            "WO-402",
            "AssignedToInProgressCommand",
            "worker1");

        beginResult.ShouldContain("In Progress");

        WorkOrder? wo;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = await context.Set<WorkOrder>().SingleAsync(w => w.Number == "WO-402");
        }

        wo.Status.ShouldBe(WorkOrderStatus.InProgress);
        wo.Assignee!.UserName.ShouldBe("worker1");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenExecutingWorkOrderCommandForMissingWorkOrder()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "MISSING-WO", "AssignedToCancelledCommand", "user1");

        result.ShouldContain("No work order found");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenExecutingUserMissing()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = "WO-501", Title = "Test", Status = WorkOrderStatus.Assigned };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-501", "AssignedToCancelledCommand", "ghost");

        result.ShouldContain("Employee with username 'ghost' not found");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenAssigneeMissingForDraftToAssigned()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role("Manager", true, false));
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-503",
            Title = "Missing assignee",
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            "WO-503",
            "DraftToAssignedCommand",
            "creator1",
            "missing-assignee");

        result.ShouldContain("Assignee with username 'missing-assignee' not found");
    }

    [Test]
    public async Task ShouldReturnErrorWhenCommandInvalidForCurrentStatus()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var draftOrder = new WorkOrder { Creator = employee, Number = "WO-504", Title = "Draft", Status = WorkOrderStatus.Draft };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "WO-504", "AssignedToCancelledCommand", "user1");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldExecuteInProgressToCompleteCommand()
    {
        var creator = new Employee("creator1", "Jane", "Creator", "creator@test.com");
        var assignee = new Employee("worker1", "Sam", "Worker", "worker@test.com");
        assignee.AddRole(new Role("Worker", false, true));
        var inProgressOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-505",
            Title = "Complete me",
            Status = WorkOrderStatus.InProgress
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(inProgressOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            "WO-505",
            "InProgressToCompleteCommand",
            "worker1");

        result.ShouldContain("Complete");

        WorkOrder? wo;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            wo = await context.Set<WorkOrder>().SingleAsync(w => w.Number == "WO-505");
        }

        wo.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    private static async Task SeedFilterWorkOrders()
    {
        var lovejoy = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "lovejoy@test.com");
        var otherCreator = new Employee("other-creator", "Other", "Creator", "creator@test.com");
        var willie = new Employee("gwillie", "Groundskeeper", "Willie", "willie@test.com");
        var otherAssignee = new Employee("other-assignee", "Other", "Assignee", "assignee@test.com");
        var workOrders = new[]
        {
            CreateWorkOrder("LJDRAFT", lovejoy, null, WorkOrderStatus.Draft),
            CreateWorkOrder("OTDRAFT", otherCreator, null, WorkOrderStatus.Draft),
            CreateWorkOrder("LJWA", lovejoy, willie, WorkOrderStatus.Assigned),
            CreateWorkOrder("LJWIP", lovejoy, willie, WorkOrderStatus.InProgress),
            CreateWorkOrder("OTWIP", otherCreator, willie, WorkOrderStatus.InProgress),
            CreateWorkOrder("LJCOMP", lovejoy, otherAssignee, WorkOrderStatus.Complete),
            CreateWorkOrder("OTASN", otherCreator, otherAssignee, WorkOrderStatus.Assigned)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(lovejoy, otherCreator, willie, otherAssignee);
        context.Set<WorkOrder>().AddRange(workOrders);
        await context.SaveChangesAsync();
    }

    private static WorkOrder CreateWorkOrder(
        string number,
        Employee creator,
        Employee? assignee,
        WorkOrderStatus status) =>
        new()
        {
            Number = number,
            Title = number,
            Description = "Filter test",
            Instructions = "Summary must not expose this",
            Creator = creator,
            Assignee = assignee,
            Status = status
        };

    private static string[] ExtractNumbers(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("Number").GetString()!)
            .ToArray();
    }

    private static string?[] ParameterNames(string methodName)
    {
        var method = typeof(WorkOrderTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new InvalidOperationException($"Method '{methodName}' was not found.");

        return method.GetParameters()
            .Select(parameter => parameter.Name)
            .ToArray();
    }
}

internal static class WorkOrderNumberAssertions
{
    public static void ShouldBeSet(this IEnumerable<string> actual, params string[] expected)
    {
        actual.OrderBy(number => number).ShouldBe(expected.OrderBy(number => number));
    }
}
