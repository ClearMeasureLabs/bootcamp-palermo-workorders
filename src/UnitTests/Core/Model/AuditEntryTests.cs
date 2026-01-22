using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class AuditEntryTests
{
    [Test]
    public void ShouldCreateAuditEntry()
    {
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = Guid.NewGuid(),
            Sequence = 1,
            ArchivedEmployeeName = "John Doe",
            Date = DateTime.Now,
            BeginStatus = WorkOrderStatus.Draft,
            EndStatus = WorkOrderStatus.Assigned
        };

        Assert.That(auditEntry.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(auditEntry.WorkOrderId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(auditEntry.Sequence, Is.EqualTo(1));
        Assert.That(auditEntry.ArchivedEmployeeName, Is.EqualTo("John Doe"));
        Assert.That(auditEntry.BeginStatus, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(auditEntry.EndStatus, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void ShouldHandleEmptyStatus()
    {
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = Guid.NewGuid(),
            Sequence = 1,
            BeginStatus = WorkOrderStatus.None,
            EndStatus = WorkOrderStatus.Draft
        };

        Assert.That(auditEntry.BeginStatus.IsEmpty(), Is.True);
        Assert.That(auditEntry.EndStatus.IsEmpty(), Is.False);
    }
}
