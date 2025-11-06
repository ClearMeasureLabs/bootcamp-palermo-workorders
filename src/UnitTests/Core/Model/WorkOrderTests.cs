using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTests
{
    [Test]
    public void PropertiesShouldInitializeToProperDefaults()
    {
        var workOrder = new WorkOrder();
        workOrder.Id.ShouldBe(Guid.Empty);
        workOrder.Title.ShouldBe(string.Empty);
        workOrder.Description.ShouldBe(string.Empty);
        workOrder.Status.ShouldBe(WorkOrderStatus.Draft);
        workOrder.Number.ShouldBeNull();
        workOrder.Creator.ShouldBeNull();
        workOrder.Assignee.ShouldBeNull();
    }

    [Test]
    public void ToStringShouldReturnWoNumber()
    {
        var order = new WorkOrder();
        order.Number = "456";
        order.ToString().ShouldBe("Work Order 456");
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

        workOrder.Id.ShouldBe(guid);
        workOrder.Title.ShouldBe("Title");
        workOrder.Description.ShouldBe("Description");
        workOrder.Status.ShouldBe(WorkOrderStatus.Complete);
        workOrder.Number.ShouldBe("Number");
        workOrder.Creator.ShouldBe(creator);
        workOrder.Assignee.ShouldBe(assignee);
    }

    [Test]
    public void ShouldShowFriendlyStatusValuesAsStrings()
    {
        var workOrder = new WorkOrder();
        workOrder.Status = WorkOrderStatus.Assigned;

        workOrder.FriendlyStatus.ShouldBe("Assigned");
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnDescription()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder();
        order.Description = longText;
        order.Description.Length.ShouldBe(4000);
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnInstructions()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder();
        order.Instructions = longText;
        order.Instructions.Length.ShouldBe(4000);
    }

    [Test]
    public void InstructionsShouldAcceptNullValue()
    {
        var order = new WorkOrder();
        order.Instructions = null;
        order.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void InstructionsShouldAcceptEmptyString()
    {
        var order = new WorkOrder();
        order.Instructions = "";
        order.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void InstructionsShouldAccept4000Characters()
    {
        var text = new string('x', 4000);
        var order = new WorkOrder();
        order.Instructions = text;
        order.Instructions.Length.ShouldBe(4000);
    }

    [Test]
    public void ShouldChangeStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        order.ChangeStatus(WorkOrderStatus.Assigned);
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }
}
