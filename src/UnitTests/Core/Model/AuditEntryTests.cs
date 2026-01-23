using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class AuditEntryTests
{
    [Test]
    public void ShouldInitializePropertiesToDefaults()
    {
        var auditEntry = new AuditEntry();

        Assert.That(auditEntry.Id, Is.EqualTo(Guid.Empty));
        Assert.That(auditEntry.WorkOrderId, Is.EqualTo(Guid.Empty));
        Assert.That(auditEntry.WorkOrder, Is.Null);
        Assert.That(auditEntry.Sequence, Is.EqualTo(0));
        Assert.That(auditEntry.EmployeeId, Is.Null);
        Assert.That(auditEntry.Employee, Is.Null);
        Assert.That(auditEntry.ArchivedEmployeeName, Is.Null);
        Assert.That(auditEntry.Date, Is.Null);
        Assert.That(auditEntry.BeginStatus, Is.Null);
        Assert.That(auditEntry.EndStatus, Is.Null);
        Assert.That(auditEntry.ActionType, Is.Null);
    }

    [Test]
    public void ShouldSetAndGetPropertiesProperly()
    {
        var id = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrder { Id = workOrderId };
        var employeeId = Guid.NewGuid();
        var employee = new Employee { Id = employeeId };
        var date = new DateTime(2025, 1, 23, 10, 0, 0);

        var auditEntry = new AuditEntry
        {
            Id = id,
            WorkOrderId = workOrderId,
            WorkOrder = workOrder,
            Sequence = 1,
            EmployeeId = employeeId,
            Employee = employee,
            ArchivedEmployeeName = "Test User",
            Date = date,
            BeginStatus = WorkOrderStatus.Draft,
            EndStatus = WorkOrderStatus.Assigned,
            ActionType = "Assign"
        };

        Assert.That(auditEntry.Id, Is.EqualTo(id));
        Assert.That(auditEntry.WorkOrderId, Is.EqualTo(workOrderId));
        Assert.That(auditEntry.WorkOrder, Is.EqualTo(workOrder));
        Assert.That(auditEntry.Sequence, Is.EqualTo(1));
        Assert.That(auditEntry.EmployeeId, Is.EqualTo(employeeId));
        Assert.That(auditEntry.Employee, Is.EqualTo(employee));
        Assert.That(auditEntry.ArchivedEmployeeName, Is.EqualTo("Test User"));
        Assert.That(auditEntry.Date, Is.EqualTo(date));
        Assert.That(auditEntry.BeginStatus, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(auditEntry.EndStatus, Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(auditEntry.ActionType, Is.EqualTo("Assign"));
    }
}
