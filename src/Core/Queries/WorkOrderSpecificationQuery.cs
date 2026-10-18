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

    public void MatchOverdueOnly(bool overdueOnly)
    {
        OverdueOnly = overdueOnly;
    }

    public void MatchDueBefore(DateOnly? dueBefore)
    {
        DueBefore = dueBefore;
    }

    public string? StatusKey { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public Employee? Assignee { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public Employee? Creator { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public bool OverdueOnly { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public DateOnly? DueBefore { get; set; }

    public WorkOrderStatus? Status => StatusKey != null ? WorkOrderStatus.FromKey(StatusKey) : null;
}
