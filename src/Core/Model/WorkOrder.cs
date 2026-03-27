namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrder : EntityBase<WorkOrder>
{
    private string? _description = "";
    private string? _instructions;

    public string? Title { get; set; } = "";

    public string? Description
    {
        get => _description;
        set => _description = getTruncatedString(value);
    }

    public string? Instructions
    {
        get => _instructions;
        set => _instructions = getTruncatedOptionalString(value);
    }

    public string? RoomNumber { get; set; } = null;

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

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

        return truncateToMaxLength(value);
    }

    private string? getTruncatedOptionalString(string? value)
    {
        if (value == null)
        {
            return null;
        }

        return truncateToMaxLength(value);
    }

    private static string truncateToMaxLength(string value)
    {
        const int maxChars = 4000;
        var maxLength = Math.Min(maxChars, value.Length);
        return value.Substring(0, maxLength);
    }

    protected string getTextForStatus()
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

    public ICollection<WorkOrderAttachment> Attachments { get; set; } = new List<WorkOrderAttachment>();
}