using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderSpecificationQuery : IRequest<WorkOrder[]>, IRemotableRequest
{
    public void MatchStatus(WorkOrderStatus? status)
    {
        StatusKey = status?.Key;
    }

    public void MatchAssignee(Employee? assignee)
    {
        Assignee = assignee;
    }

    public void MatchCreator(Employee? creator)
    {
        Creator = creator;
    }

    public void MatchPriority(WorkOrderPriority? priority)
    {
        Priority = priority;
    }

    public void MatchRecurring(bool? isRecurring)
    {
        IsRecurring = isRecurring;
    }

    public void MatchCategory(WorkOrderCategory? category)
    {
        Category = category;
    }

    public string? StatusKey { get; set; }

    public Employee? Assignee { get; set; }

    public Employee? Creator { get; set; }
    
    public WorkOrderPriority? Priority { get; set; }
    
    public WorkOrderCategory? Category { get; set; }
    
    public bool? IsRecurring { get; set; }
    
    public WorkOrderStatus? Status => StatusKey != null ? WorkOrderStatus.FromKey(StatusKey) : null;
}