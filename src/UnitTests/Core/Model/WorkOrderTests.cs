using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTests
{
    [Test]
    public void PropertiesShouldInitializeToProperDefaults()
    {
        var workOrder = new WorkOrder();
        Assert.That(workOrder.Id, Is.EqualTo(Guid.Empty));
        Assert.That(workOrder.Title, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Description, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(workOrder.Number, Is.EqualTo(null));
        Assert.That(workOrder.Creator, Is.EqualTo(null));
        Assert.That(workOrder.Assignee, Is.EqualTo(null));
    }

    [Test]
    public void ToStringShouldReturnWoNumber()
    {
        var order = new WorkOrder();
        order.Number = "456";
        Assert.That(order.ToString(), Is.EqualTo("Work Order 456"));
    }

    [Test]
    public void PropertiesShouldGetAndSetValuesProperly()
    {
        var workOrder = new WorkOrder();
        var guid = Guid.NewGuid();
        var creator = new Employee();
        var assignee = new Employee();
        var createdDate = new DateTime(2000, 1, 1);
        var completedDate = new DateTime(2000, 10, 1);
        var auditDate = new DateTime(2000, 1, 1, 8, 0, 0);

        workOrder.Id = guid;
        workOrder.Title = "Title";
        workOrder.Description = "Description";
        workOrder.Status = WorkOrderStatus.Complete;
        workOrder.Number = "Number";
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;

        Assert.That(workOrder.Id, Is.EqualTo(guid));
        Assert.That(workOrder.Title, Is.EqualTo("Title"));
        Assert.That(workOrder.Description, Is.EqualTo("DESCRIPTION"));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(workOrder.Number, Is.EqualTo("Number"));
        Assert.That(workOrder.Creator, Is.EqualTo(creator));
        Assert.That(workOrder.Assignee, Is.EqualTo(assignee));
    }

    [Test]
    public void ShouldShowFriendlyStatusValuesAsStrings()
    {
        var workOrder = new WorkOrder();
        workOrder.Status = WorkOrderStatus.Assigned;

        Assert.That(workOrder.FriendlyStatus, Is.EqualTo("Assigned"));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnDescription()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder();
        order.Description = longText;
        Assert.That(order.Description.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldChangeStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        order.ChangeStatus(WorkOrderStatus.Assigned);
        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void ShouldForceDescriptionToUpperCase()
    {
        var order = new WorkOrder();
        order.Description = "hello world";
        Assert.That(order.Description, Is.EqualTo("HELLO WORLD"));
    }

    [Test]
    public void ShouldForceDescriptionToUpperCaseWithMixedCase()
    {
        var order = new WorkOrder();
        order.Description = "Fix the Broken Pipe in Room 101";
        Assert.That(order.Description, Is.EqualTo("FIX THE BROKEN PIPE IN ROOM 101"));
    }

    [Test]
    public void ShouldHandleUnicodeAtTruncationBoundary()
    {
        // Verify that non-ASCII characters at the truncation boundary are handled
        // correctly. In .NET 10, ß.ToUpperInvariant() returns ß (no expansion),
        // so the string stays at 4000 chars. The re-truncation guard in the setter
        // is defensive for future runtime changes where uppercasing could expand length.
        var prefix = new string('a', 3999);
        var input = prefix + "\u00DF"; // 4000 chars, last char is ß
        var order = new WorkOrder();
        order.Description = input;
        Assert.That(order.Description.Length, Is.EqualTo(4000));
        Assert.That(order.Description, Does.StartWith(new string('A', 3999)));
        Assert.That(order.Description[3999], Is.EqualTo('\u00DF'));
    }
}