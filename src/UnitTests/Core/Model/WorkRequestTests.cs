using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkRequestTests
{
    [Test]
    public void PropertiesShouldInitializeToProperDefaults()
    {
        var workRequest = new WorkRequest();
        Assert.That(workRequest.Id, Is.EqualTo(Guid.Empty));
        Assert.That(workRequest.Title, Is.EqualTo(string.Empty));
        Assert.That(workRequest.Description, Is.EqualTo(string.Empty));
        Assert.That(workRequest.Instructions, Is.EqualTo(string.Empty));
        Assert.That(workRequest.Status, Is.EqualTo(WorkRequestStatus.Draft));
        Assert.That(workRequest.Number, Is.EqualTo(null));
        Assert.That(workRequest.Creator, Is.EqualTo(null));
        Assert.That(workRequest.Assignee, Is.EqualTo(null));
    }

    [Test]
    public void ToStringShouldReturnWoNumber()
    {
        var order = new WorkRequest();
        order.Number = "456";
        Assert.That(order.ToString(), Is.EqualTo("Work Request 456"));
    }

    [Test]
    public void PropertiesShouldGetAndSetValuesProperly()
    {
        var workRequest = new WorkRequest();
        var guid = Guid.NewGuid();
        var creator = new Employee();
        var assignee = new Employee();
        var createdDate = new DateTime(2000, 1, 1);
        var completedDate = new DateTime(2000, 10, 1);
        var auditDate = new DateTime(2000, 1, 1, 8, 0, 0);

        workRequest.Id = guid;
        workRequest.Title = "Title";
        workRequest.Description = "Description";
        workRequest.Instructions = "Bring ladder and safety gear";
        workRequest.Status = WorkRequestStatus.Complete;
        workRequest.Number = "Number";
        workRequest.Creator = creator;
        workRequest.Assignee = assignee;

        Assert.That(workRequest.Id, Is.EqualTo(guid));
        Assert.That(workRequest.Title, Is.EqualTo("Title"));
        Assert.That(workRequest.Description, Is.EqualTo("Description"));
        Assert.That(workRequest.Instructions, Is.EqualTo("Bring ladder and safety gear"));
        Assert.That(workRequest.Status, Is.EqualTo(WorkRequestStatus.Complete));
        Assert.That(workRequest.Number, Is.EqualTo("Number"));
        Assert.That(workRequest.Creator, Is.EqualTo(creator));
        Assert.That(workRequest.Assignee, Is.EqualTo(assignee));
    }

    [Test]
    public void ShouldShowFriendlyStatusValuesAsStrings()
    {
        var workRequest = new WorkRequest();
        workRequest.Status = WorkRequestStatus.Assigned;

        Assert.That(workRequest.FriendlyStatus, Is.EqualTo("Assigned"));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnDescription()
    {
        var longText = new string('x', 4001);
        var order = new WorkRequest();
        order.Description = longText;
        Assert.That(order.Description.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnInstructions()
    {
        var longText = new string('x', 4001);
        var order = new WorkRequest();
        order.Instructions = longText;
        Assert.That(order.Instructions.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldReturnEmptyStringWhenInstructionsSetToNull()
    {
        var order = new WorkRequest();
        order.Instructions = null;
        Assert.That(order.Instructions, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ShouldChangeStatus()
    {
        var order = new WorkRequest();
        order.Status = WorkRequestStatus.Draft;
        order.ChangeStatus(WorkRequestStatus.Assigned);
        Assert.That(order.Status, Is.EqualTo(WorkRequestStatus.Assigned));
    }
}