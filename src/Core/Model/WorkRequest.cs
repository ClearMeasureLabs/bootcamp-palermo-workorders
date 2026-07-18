namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkRequest : EntityBase<WorkRequest>
{
    private string? _description = "";
    private string? _instructions = "";

    public string? Title { get; set; } = "";

    public string? Description
    {
        get => _description;
        set => _description = getTruncatedString(value);
    }

    public string? Instructions
    {
        get => _instructions;
        set => _instructions = getTruncatedString(value);
    }

    public string? RoomNumber { get; set; } = null;

    public WorkRequestStatus Status { get; set; } = WorkRequestStatus.Draft;

    public Employee? Creator { get; set; } = null;

    public Employee? Assignee { get; set; } = null;

    public string? Number { get; set; } = null!;

    public string FriendlyStatus => getTextForStatus();


    public DateTime? AssignedDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    private string? getTruncatedString(string? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var maxLength = Math.Min(4000, value.Length);
        return value.Substring(0, maxLength);
    }

    protected string getTextForStatus()
    {
        return Status.ToString();
    }

    public override Guid Id { get; set; }

    public override string ToString()
    {
        return "Work Request " + Number;
    }

    public void ChangeStatus(WorkRequestStatus status)
    {
        Status = status;
    }

    public void ChangeStatus(Employee employee, DateTime date, WorkRequestStatus status)
    {
        Status = status;
    }

    public string GetMessage()
    {
        return "Work Request " + Number + " is now in Status " + Status;
    }

    public bool CanReassign()
    {
        return Status == WorkRequestStatus.Draft;
    }

    public ICollection<WorkRequestAttachment> Attachments { get; set; } = new List<WorkRequestAttachment>();
}