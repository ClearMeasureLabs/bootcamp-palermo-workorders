using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

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
    public void GetAgeLabel_WhenCreatedDateIsNull_ReturnsEmpty()
    {
        var order = new WorkOrder { CreatedDate = null };
        order.GetAgeLabel(DateTime.UtcNow).ShouldBe(string.Empty);
    }

    [Test]
    public void GetAgeLabel_WhenSameDay_ReturnsToday()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddHours(-3) };
        order.GetAgeLabel(now).ShouldBe("today");
    }

    [Test]
    public void GetAgeLabel_WhenOneDay_Returns1Day()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-1) };
        order.GetAgeLabel(now).ShouldBe("1 day");
    }

    [Test]
    public void GetAgeLabel_WhenTwoDays_Returns2Days()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-2) };
        order.GetAgeLabel(now).ShouldBe("2 days");
    }

    [Test]
    public void GetAgeLabel_WhenSixDays_Returns6Days()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-6) };
        order.GetAgeLabel(now).ShouldBe("6 days");
    }

    [Test]
    public void GetAgeLabel_WhenSevenDays_Returns1Week()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-7) };
        order.GetAgeLabel(now).ShouldBe("1 week");
    }

    [Test]
    public void GetAgeLabel_WhenThirteenDays_Returns1Week()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-13) };
        order.GetAgeLabel(now).ShouldBe("1 week");
    }

    [Test]
    public void GetAgeLabel_WhenFourteenDays_Returns2Weeks()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-14) };
        order.GetAgeLabel(now).ShouldBe("2 weeks");
    }

    [Test]
    public void GetAgeLabel_WhenTwentyEightDays_Returns4Weeks()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-28) };
        order.GetAgeLabel(now).ShouldBe("4 weeks");
    }

    [Test]
    public void GetAgeLabel_WhenThirtyFiveDays_ReturnsOverAMonth()
    {
        var now = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var order = new WorkOrder { CreatedDate = now.AddDays(-35) };
        order.GetAgeLabel(now).ShouldBe("over a month");
    }
}
