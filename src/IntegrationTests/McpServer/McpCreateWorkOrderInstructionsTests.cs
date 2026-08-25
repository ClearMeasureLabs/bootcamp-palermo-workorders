using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpCreateWorkOrderInstructionsTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldPersistExplicitInstructionsDistinctFromDescription()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        const string description = "Fix the broken window in the fellowship hall";
        const string instructions = "Bring ladder from east shed; do not block wheelchair ramp";

        var result = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "New Work Order",
            description,
            "creator1",
            instructions: instructions);

        result.ShouldContain("New Work Order");
        var number = ExtractWorkOrderNumber(result);
        number.ShouldNotBeNullOrWhiteSpace();

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number!));
        reloaded.ShouldNotBeNull();
        reloaded!.Instructions.ShouldBe(instructions);
        reloaded.Instructions.ShouldNotBe(description);
        reloaded.Description.ShouldBe(description);
    }

    [Test]
    public async Task ShouldPersistEmptyInstructionsWhenOmitted()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();

        var result = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Omitted instructions",
            "Description only",
            "creator1");

        var number = ExtractWorkOrderNumber(result);
        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number!));
        reloaded!.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldPersistEmptyInstructionsWhenEmpty()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();

        var result = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Empty instructions",
            "Description only",
            "creator1",
            instructions: string.Empty);

        var number = ExtractWorkOrderNumber(result);
        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number!));
        reloaded!.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldTruncateInstructionsTo4000CharactersOnCreate()
    {
        await SeedCreatorAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var longInstructions = new string('I', WorkOrder.InstructionsMaxLength + 1);
        var expected = new string('I', WorkOrder.InstructionsMaxLength);

        var result = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Long instructions",
            "Description",
            "creator1",
            instructions: longInstructions);

        result.ShouldContain("Long instructions");
        var number = ExtractWorkOrderNumber(result);
        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number!));
        reloaded!.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
        reloaded.Instructions.ShouldBe(expected);
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenCreatorMissing()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();

        var result = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Title",
            "Description",
            "nonexistent_user",
            instructions: "Some instructions");

        result.ShouldBe("Employee with username 'nonexistent_user' not found.");
    }

    private static async Task SeedCreatorAsync()
    {
        var employee = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        await using var context = TestHost.GetRequiredService<Microsoft.EntityFrameworkCore.DbContext>();
        context.Add(employee);
        await context.SaveChangesAsync();
    }

    private static string? ExtractWorkOrderNumber(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("Number").GetString();
    }
}
