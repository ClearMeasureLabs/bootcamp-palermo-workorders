// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrder : EntityBase<WorkOrder>
{
    public const int RoomNumberMaxLength = 900;

    public const int DescriptionMaxLength = 4000;

    public const int InstructionsMaxLength = 4000;

    public string? Title { get; set; } = "";

    public string? Description
    {
        get;
        set => field = GetTruncatedString(value);
    } = "";

    public string? Instructions
    {
        get;
        set => field = GetTruncatedString(value);
    } = "";

    public string? RoomNumber { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    public Employee? Creator { get; set; }

    public Employee? Assignee { get; set; }

    public string? Number { get; set; }

    public string FriendlyStatus => GetTextForStatus();


    public DateTime? AssignedDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public DateOnly? DueDate { get; set; }

    private string GetTruncatedString(string? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var maxLength = Math.Min(InstructionsMaxLength, value.Length);
        return value.Substring(0, maxLength);
    }

    protected string GetTextForStatus()
    {
        return Status.ToString();
    }

    public override Guid Id { get; set; }

    public override string ToString()
    {
        return "Work Order " + Number;
    }

    public void ChangeStatus(WorkOrderStatus status)
    {
        Status = status;
    }

    public void ChangeStatus(Employee employee, DateTime date, WorkOrderStatus status)
    {
        Status = status;
    }

    public string GetMessage()
    {
        return "Work Order " + Number + " is now in Status " + Status;
    }

    public bool CanReassign()
    {
        return Status == WorkOrderStatus.Draft;
    }

    public DueDateUrgency GetDueDateUrgency() =>
        DueDateUrgencyCalculator.Calculate(this, TimeProvider.System);

    public bool IsOverdue() => GetDueDateUrgency() == DueDateUrgency.Overdue;

    public ICollection<WorkOrderAttachment> Attachments { get; set; } = new List<WorkOrderAttachment>();
}