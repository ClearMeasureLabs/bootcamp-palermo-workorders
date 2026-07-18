using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkRequestSpecificationQuery : IRequest<WorkRequest[]>, IRemotableRequest
{
    public void MatchStatus(WorkRequestStatus? status)
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

    public string? StatusKey { get; set; }

    public Employee? Assignee { get; set; }

    public Employee? Creator { get; set; }
    public WorkRequestStatus? Status => StatusKey != null ? WorkRequestStatus.FromKey(StatusKey) : null;
}