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
        Assert.That(workOrder.Instructions, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(workOrder.Number, Is.EqualTo(null));
        Assert.That(workOrder.Creator, Is.EqualTo(null));
        Assert.That(workOrder.Assignee, Is.EqualTo(null));
        Assert.That(workOrder.DueDate, Is.Null);
    }

    [Test]
    public void ToStringShouldReturnWoNumber()
    {
        var order = new WorkOrder() { Number = "456" };
        Assert.That(order.ToString(), Is.EqualTo("Work Order 456"));
    }

    [Test]
    public void PropertiesShouldGetAndSetValuesProperly()
    {
        var workOrder = new WorkOrder();
        var guid = Guid.NewGuid();
        var creator = new Employee();
        var assignee = new Employee();

        workOrder.Id = guid;
        workOrder.Title = "Title";
        workOrder.Description = "Description";
        workOrder.Instructions = "Bring ladder and safety gear";
        workOrder.Status = WorkOrderStatus.Complete;
        workOrder.Number = "Number";
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;

        Assert.That(workOrder.Id, Is.EqualTo(guid));
        Assert.That(workOrder.Title, Is.EqualTo("Title"));
        Assert.That(workOrder.Description, Is.EqualTo("Description"));
        Assert.That(workOrder.Instructions, Is.EqualTo("Bring ladder and safety gear"));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(workOrder.Number, Is.EqualTo("Number"));
        Assert.That(workOrder.Creator, Is.EqualTo(creator));
        Assert.That(workOrder.Assignee, Is.EqualTo(assignee));
    }

    [Test]
    public void ShouldShowFriendlyStatusValuesAsStrings()
    {
        var workOrder = new WorkOrder() { Status = WorkOrderStatus.Assigned };

        Assert.That(workOrder.FriendlyStatus, Is.EqualTo("Assigned"));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnDescription()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder() { Description = longText };
        Assert.That(order.Description.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnInstructions()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder() { Instructions = longText };
        Assert.That(order.Instructions.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldReturnEmptyStringWhenInstructionsSetToNull()
    {
        var order = new WorkOrder() { Instructions = null };
        Assert.That(order.Instructions, Is.EqualTo(string.Empty));
    }

    [Test]
    public void InstructionsMaxLength_ShouldBe4000()
    {
        Assert.That(WorkOrder.InstructionsMaxLength, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldChangeStatus()
    {
        var order = new WorkOrder() { Status = WorkOrderStatus.Draft };
        order.ChangeStatus(WorkOrderStatus.Assigned);
        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void GetAgeInDays_WhenCreatedDateIsNull_ShouldReturnNull()
    {
        var order = new WorkOrder { CreatedDate = null };
        var time = new StubTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.That(order.GetAgeInDays(time), Is.Null);
    }

    [Test]
    public void GetAgeInDays_WhenSameDay_ShouldReturnZero()
    {
        var created = new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = created };
        var time = new StubTimeProvider(new DateTimeOffset(2024, 6, 15, 20, 0, 0, TimeSpan.Zero));

        Assert.That(order.GetAgeInDays(time), Is.EqualTo(0));
    }

    [Test]
    public void GetAgeInDays_WhenThreeDaysOld_ShouldReturnThree()
    {
        var created = new DateTime(2024, 6, 12, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = created };
        var time = new StubTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.That(order.GetAgeInDays(time), Is.EqualTo(3));
    }

    [Test]
    public void GetAgeInDays_WhenCreatedDateIsFuture_ShouldReturnZero()
    {
        var created = new DateTime(2024, 6, 20, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = created };
        var time = new StubTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.That(order.GetAgeInDays(time), Is.EqualTo(0));
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
