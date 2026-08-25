using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpSaveWorkOrderTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldChangeTitleAndClearDueDateWhilePreservingOtherFields()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        const string instructions = "Check preschool gate latch before mowing";
        const string room = "Front lawn";

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "mow front grass",
            "Weekly mowing",
            "creator1",
            roomNumber: room,
            dueDate: "2026-09-12",
            instructions: instructions);

        var number = ExtractWorkOrderNumber(createResult);
        number.ShouldNotBeNullOrWhiteSpace();

        var saveResult = await WorkOrderTools.SaveWorkOrder(
            bus,
            number!,
            "creator1",
            title: "Saturday mow",
            dueDate: string.Empty);

        saveResult.ShouldContain("Saturday mow");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number!));
        reloaded.ShouldNotBeNull();
        reloaded!.Title.ShouldBe("Saturday mow");
        reloaded.DueDate.ShouldBeNull();
        reloaded.Instructions.ShouldBe(instructions);
        reloaded.RoomNumber.ShouldBe(room);
        reloaded.Status.ShouldBe(WorkOrderStatus.Draft);
        reloaded.Number.ShouldBe(number);
        reloaded.Creator!.UserName.ShouldBe("creator1");
        reloaded.Assignee.ShouldBeNull();
    }

    [Test]
    public async Task ShouldLeaveOmittedFieldsUnchangedWhenOnlyRoomNumberProvided()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        const string title = "Original title";
        const string description = "Original description";
        const string instructions = "Original instructions";
        var dueDate = new DateOnly(2026, 10, 4);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            title,
            description,
            "creator1",
            roomNumber: "Room A",
            dueDate: dueDate.ToString("yyyy-MM-dd"),
            instructions: instructions);

        var number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(bus, number, "creator1", roomNumber: "Room B");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe(title);
        reloaded.Description.ShouldBe(description);
        reloaded.Instructions.ShouldBe(instructions);
        reloaded.DueDate.ShouldBe(dueDate);
        reloaded.RoomNumber.ShouldBe("Room B");
    }

    [Test]
    public async Task ShouldPersistEmptyDescriptionInstructionsAndRoom()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Has content",
            "Has description",
            "creator1",
            roomNumber: "Room 101",
            instructions: "Has instructions");

        var number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            "creator1",
            description: string.Empty,
            instructions: "   ",
            roomNumber: string.Empty);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Description.ShouldBe(string.Empty);
        reloaded.Instructions.ShouldBe(string.Empty);
        reloaded.RoomNumber.ShouldBe(string.Empty);
        reloaded.Title.ShouldBe("Has content");
    }

    [Test]
    public async Task ShouldPersistValidDueDate()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var expectedDueDate = new DateOnly(2026, 11, 15);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Due date save",
            "Description",
            "creator1");

        var number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            "creator1",
            dueDate: expectedDueDate.ToString("yyyy-MM-dd"));

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.DueDate.ShouldBe(expectedDueDate);
    }

    [Test]
    public async Task ShouldRejectInvalidDueDateWithoutChangingStoredDueDate()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var originalDueDate = new DateOnly(2026, 8, 30);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Invalid due date",
            "Description",
            "creator1",
            dueDate: originalDueDate.ToString("yyyy-MM-dd"));

        var number = ExtractWorkOrderNumber(createResult)!;

        var result = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            "creator1",
            dueDate: "not-a-date");

        result.ShouldBe("Invalid due date 'not-a-date'. Use yyyy-MM-dd.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldRejectEmptyTitleWithoutChangingStoredRow()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var originalDueDate = new DateOnly(2026, 9, 1);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Keep this title",
            "Description",
            "creator1",
            dueDate: originalDueDate.ToString("yyyy-MM-dd"));

        var number = ExtractWorkOrderNumber(createResult)!;

        var result = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            "creator1",
            title: string.Empty);

        result.ShouldBe("The Title field is required.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe("Keep this title");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldRejectWhitespaceTitleAndNotPersistDueDateInSameCall()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var originalDueDate = new DateOnly(2026, 9, 2);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Keep this title",
            "Description",
            "creator1",
            dueDate: originalDueDate.ToString("yyyy-MM-dd"));

        var number = ExtractWorkOrderNumber(createResult)!;

        var result = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            "creator1",
            title: "   ",
            dueDate: "2026-12-25");

        result.ShouldBe("The Title field is required.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe("Keep this title");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldTruncateInstructionsTo4000Characters()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var longInstructions = new string('x', WorkOrder.InstructionsMaxLength + 1);
        var expected = new string('x', WorkOrder.InstructionsMaxLength);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Truncate instructions",
            "Description",
            "creator1");

        var number = ExtractWorkOrderNumber(createResult)!;

        var saveResult = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            "creator1",
            instructions: longInstructions);

        saveResult.ShouldContain("Truncate instructions");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
        reloaded.Instructions.ShouldBe(expected);
    }

    [Test]
    public async Task ShouldTruncateRoomNumberTo900Characters()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var longRoom = new string('r', WorkOrder.RoomNumberMaxLength + 1);
        var expected = new string('r', WorkOrder.RoomNumberMaxLength);

        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Truncate room",
            "Description",
            "creator1");

        var number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(bus, number, "creator1", roomNumber: longRoom);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberMaxLength);
        reloaded.RoomNumber.ShouldBe(expected);
    }

    [Test]
    public async Task ShouldFailForAssignedWorkOrderWithoutChangingFields()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var assignedOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-910",
            Title = "Assigned title",
            Description = "Assigned description",
            Instructions = "Assigned instructions",
            RoomNumber = "Room 1",
            DueDate = new DateOnly(2026, 9, 3),
            Status = WorkOrderStatus.Assigned
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignedOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.SaveWorkOrder(
            bus,
            "WO-910",
            "creator1",
            title: "New title");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Assigned");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-910"));
        reloaded!.Title.ShouldBe("Assigned title");
        reloaded.Description.ShouldBe("Assigned description");
        reloaded.Instructions.ShouldBe("Assigned instructions");
        reloaded.RoomNumber.ShouldBe("Room 1");
        reloaded.DueDate.ShouldBe(new DateOnly(2026, 9, 3));
        reloaded.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public async Task ShouldFailForCompleteWorkOrderWithoutChangingFields()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var completeOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-911",
            Title = "Complete title",
            Status = WorkOrderStatus.Complete
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(completeOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.SaveWorkOrder(
            bus,
            "WO-911",
            "creator1",
            title: "New title");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Complete");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-911"));
        reloaded!.Title.ShouldBe("Complete title");
        reloaded.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    [Test]
    public async Task ShouldFailForNonCreatorWithoutChangingFields()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var otherUser = new Employee("other1", "Other", "User", "other@test.com");
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-912",
            Title = "Draft title",
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(otherUser);
            context.Add(draftOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.SaveWorkOrder(
            bus,
            "WO-912",
            "other1",
            title: "New title");

        result.ShouldContain("cannot be executed");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-912"));
        reloaded!.Title.ShouldBe("Draft title");
    }

    [Test]
    public async Task ShouldFailWhenWorkOrderNumberMissing()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.SaveWorkOrder(bus, string.Empty, "creator1", title: "New title");
        result.ShouldBe("Work order number is required.");
    }

    [Test]
    public async Task ShouldFailWhenWorkOrderNumberUnknown()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.SaveWorkOrder(bus, "UNKNOWN-WO", "creator1", title: "New title");
        result.ShouldBe("No work order found with number 'UNKNOWN-WO'.");
    }

    [Test]
    public async Task ShouldFailWhenExecutingUserUnknown()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Title",
            "Description",
            "creator1");
        var number = ExtractWorkOrderNumber(createResult)!;

        var result = await WorkOrderTools.SaveWorkOrder(bus, number, "missing-user", title: "New title");
        result.ShouldBe("Employee with username 'missing-user' not found.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe("Title");
    }

    [Test]
    public void ShouldNotExposeSaveOnExecuteWorkOrderCommand()
    {
        var executorType = typeof(WorkOrderCommandExecutor);
        var field = executorType.GetField("CommandFactories", BindingFlags.NonPublic | BindingFlags.Static);
        field.ShouldNotBeNull();

        var factories = (IReadOnlyDictionary<string, Func<WorkOrder, Employee, StateCommandBase>>)field!.GetValue(null)!;
        factories.Keys.ShouldNotContain("Save");
        factories.Keys.ShouldNotContain("SaveDraftCommand");
        factories.Keys.ShouldNotContain(SaveDraftCommand.Name);
    }

    private static async Task SeedCreatorAsync()
    {
        var employee = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        await context.SaveChangesAsync();
    }

    private static string? ExtractWorkOrderNumber(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("Number").GetString();
    }
}
