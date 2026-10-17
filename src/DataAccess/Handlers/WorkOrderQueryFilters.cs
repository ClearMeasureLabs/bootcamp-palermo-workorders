using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

internal static class WorkOrderQueryFilters
{
    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        Employee? assignee,
        Employee? creator,
        WorkOrderStatus? status,
        bool overdueOnly = false,
        string? titleContains = null)
    {
        if (assignee != null)
        {
            query = query.Where(wo => wo.Assignee == assignee);
        }

        if (creator != null)
        {
            query = query.Where(wo => wo.Creator == creator);
        }

        if (status != null)
        {
            query = query.Where(wo => wo.Status == status);
        }

        if (overdueOnly)
        {
            query = ApplyOverdueFilter(query);
        }

        return ApplyTitleContainsFilter(query, titleContains);
    }

    private static IQueryable<WorkOrder> ApplyOverdueFilter(IQueryable<WorkOrder> query)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        return query.Where(wo =>
            wo.DueDate < today &&
            (wo.Status == draft || wo.Status == assigned || wo.Status == inProgress));
    }

    private static IQueryable<WorkOrder> ApplyTitleContainsFilter(
        IQueryable<WorkOrder> query,
        string? titleContains)
    {
        if (string.IsNullOrWhiteSpace(titleContains))
        {
            return query;
        }

        var lowered = titleContains.ToLowerInvariant();
        return query.Where(wo => wo.Title != null && wo.Title.ToLower().Contains(lowered));
    }

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSearchSpecification specification) =>
        Apply(query, specification.Assignee, specification.Creator, specification.Status,
            titleContains: specification.TitleContains);

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSpecificationQuery specification) =>
        Apply(query, specification.Assignee, specification.Creator, specification.Status,
            specification.OverdueOnly, specification.TitleContains);
}
