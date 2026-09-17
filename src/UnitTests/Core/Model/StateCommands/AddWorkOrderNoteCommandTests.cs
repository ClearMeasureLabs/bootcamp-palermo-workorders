using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class AddWorkOrderNoteCommandTests
{
    [Test]
    public void ShouldThrow_WhenTextIsEmpty()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var author = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var command = new AddWorkOrderNoteCommand(workOrder, author, string.Empty);

        Should.Throw<ArgumentException>(() => command.CreateNote(DateTime.UtcNow));
    }

    [Test]
    public void ShouldThrow_WhenTextIsWhitespace()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var author = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var command = new AddWorkOrderNoteCommand(workOrder, author, "   ");

        Should.Throw<ArgumentException>(() => command.CreateNote(DateTime.UtcNow));
    }
}
