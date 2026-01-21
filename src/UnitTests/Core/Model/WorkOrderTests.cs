using ClearMeasure.Bootcamp.Core.Model;
using System.ComponentModel.DataAnnotations;

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
        Assert.That(workOrder.Description, Is.EqualTo("Description"));
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
    public void ValidationShouldFail_WhenTitleIsNull()
    {
        var workOrder = new WorkOrder
        {
            Title = null,
            Description = "Valid description"
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(workOrder);
        var isValid = Validator.TryValidateObject(workOrder, context, validationResults, true);

        Assert.That(isValid, Is.False);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Title")), Is.True);
    }

    [Test]
    public void ValidationShouldFail_WhenTitleIsEmpty()
    {
        var workOrder = new WorkOrder
        {
            Title = "",
            Description = "Valid description"
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(workOrder);
        var isValid = Validator.TryValidateObject(workOrder, context, validationResults, true);

        Assert.That(isValid, Is.False);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Title")), Is.True);
    }

    [Test]
    public void ValidationShouldFail_WhenDescriptionIsNull()
    {
        var workOrder = new WorkOrder
        {
            Title = "Valid title",
            Description = null
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(workOrder);
        var isValid = Validator.TryValidateObject(workOrder, context, validationResults, true);

        Assert.That(isValid, Is.False);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Description")), Is.True);
    }

    [Test]
    public void ValidationShouldFail_WhenDescriptionIsEmpty()
    {
        var workOrder = new WorkOrder
        {
            Title = "Valid title",
            Description = ""
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(workOrder);
        var isValid = Validator.TryValidateObject(workOrder, context, validationResults, true);

        Assert.That(isValid, Is.False);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Description")), Is.True);
    }

    [Test]
    public void ValidationShouldSucceed_WhenTitleAndDescriptionAreValid()
    {
        var workOrder = new WorkOrder
        {
            Title = "Valid title",
            Description = "Valid description"
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(workOrder);
        var isValid = Validator.TryValidateObject(workOrder, context, validationResults, true);

        Assert.That(isValid, Is.True);
        Assert.That(validationResults.Count, Is.EqualTo(0));
    }
}