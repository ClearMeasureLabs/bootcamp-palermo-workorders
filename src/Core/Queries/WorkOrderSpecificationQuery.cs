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

    public void MatchRoom(Room? room)
    {
        Room = room;
    }

    public string? StatusKey { get; set; }

    public Employee? Assignee { get; set; }

    public Employee? Creator { get; set; }
    
    public Room? Room { get; set; }
    
    public WorkOrderStatus? Status => StatusKey != null ? WorkOrderStatus.FromKey(StatusKey) : null;
}