namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrder : EntityBase<WorkOrder>
{
    private string? _description = "";
    private string? _instructions = "";

    public string? Title { get; set; } = "";

    public string? Description
    {
        get => _description;
        set => _description = getTruncatedString(value, 4000);
    }

    public string? Instructions
    {
        get => _instructions;
        // Automatically truncates input to 3000 characters to match database column constraint
        set => _instructions = getTruncatedString(value, 3000);
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

    private string? getTruncatedString(string? value, int maxLength)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var length = Math.Min(maxLength, value.Length);
        return value.Substring(0, length);
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
}