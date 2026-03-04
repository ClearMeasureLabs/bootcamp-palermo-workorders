using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class SubtaskCommandHandlerTests : IntegratedTestBase
{
    private WorkOrder _workOrder = null!;
    private Employee _currentUser = null!;

    [SetUp]
    public async Task SetUp()
    {
        new DatabaseTests().Clean();

        _currentUser = Faker<Employee>();
        _currentUser.Id = Guid.NewGuid();

        _workOrder = Faker<WorkOrder>();
        _workOrder.Id = Guid.NewGuid();
        _workOrder.Creator = _currentUser;

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(_currentUser);
        context.Add(_workOrder);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task AddSubtaskCommand_ShouldPersistSubtask()
    {
        var handler = TestHost.GetRequiredService<AddSubtaskCommandHandler>();
        var command = new AddSubtaskCommand(_workOrder.Id, "Replace light fixture", 0);

        var subtask = await handler.Handle(command);

        subtask.Id.ShouldNotBe(Guid.Empty);
        subtask.Title.ShouldBe("Replace light fixture");
        subtask.IsCompleted.ShouldBeFalse();

        await using var context = TestHost.GetRequiredService<DbContext>();
        var persisted = await context.Set<WorkOrderSubtask>().SingleOrDefaultAsync(s => s.Id == subtask.Id);
        persisted.ShouldNotBeNull();
        persisted!.WorkOrderId.ShouldBe(_workOrder.Id);
        persisted.Title.ShouldBe("Replace light fixture");
        persisted.IsCompleted.ShouldBeFalse();
    }

    [Test]
    public async Task ToggleSubtaskCommand_ShouldPersistCompletionState()
    {
        var addHandler = TestHost.GetRequiredService<AddSubtaskCommandHandler>();
        var subtask = await addHandler.Handle(new AddSubtaskCommand(_workOrder.Id, "Check wiring", 0));

        var toggleHandler = TestHost.GetRequiredService<ToggleSubtaskCommandHandler>();
        var toggled = await toggleHandler.Handle(new ToggleSubtaskCommand(subtask.Id));

        toggled.IsCompleted.ShouldBeTrue();

        await using var context = TestHost.GetRequiredService<DbContext>();
        var persisted = await context.Set<WorkOrderSubtask>().SingleAsync(s => s.Id == subtask.Id);
        persisted.IsCompleted.ShouldBeTrue();
    }

    [Test]
    public async Task RemoveSubtaskCommand_ShouldDeleteSubtask()
    {
        var addHandler = TestHost.GetRequiredService<AddSubtaskCommandHandler>();
        var subtask = await addHandler.Handle(new AddSubtaskCommand(_workOrder.Id, "Install fixture", 0));

        var removeHandler = TestHost.GetRequiredService<RemoveSubtaskCommandHandler>();
        var result = await removeHandler.Handle(new RemoveSubtaskCommand(subtask.Id));

        result.ShouldBeTrue();

        await using var context = TestHost.GetRequiredService<DbContext>();
        var persisted = await context.Set<WorkOrderSubtask>().SingleOrDefaultAsync(s => s.Id == subtask.Id);
        persisted.ShouldBeNull();
    }
}
