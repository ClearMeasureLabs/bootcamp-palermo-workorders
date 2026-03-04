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

    public void MatchBuilding(string? building)
    {
        Building = building;
    }

    public void MatchFloor(string? floor)
    {
        Floor = floor;
    }

    public string? StatusKey { get; set; }

    public Employee? Assignee { get; set; }

    public Employee? Creator { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public WorkOrderStatus? Status => StatusKey != null ? WorkOrderStatus.FromKey(StatusKey) : null;
}