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
        bool overdueOnly = false)
    {
        if (assignee != null)
        {
            query = query.Where(wo => wo.Assignee == assignee);
        }

        if (creator != null)
        {
            query = query.Where(wo => wo.Creator == creator);
        }

        query = status != null
            ? query.Where(wo => wo.Status == status)
            : query.Where(wo => wo.Status != WorkOrderStatus.Cancelled);

        if (overdueOnly)
        {
            query = ApplyOverdueFilter(query);
        }

        return query;
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

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSearchSpecification specification) =>
        Apply(query, specification.Assignee, specification.Creator, specification.Status);

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSpecificationQuery specification) =>
        Apply(query, specification.Assignee, specification.Creator, specification.Status, specification.OverdueOnly);
}
