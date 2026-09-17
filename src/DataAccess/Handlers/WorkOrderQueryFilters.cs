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

        if (status != null)
        {
            query = query.Where(wo => wo.Status == status);
        }

        if (overdueOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            query = query.Where(wo =>
                wo.DueDate != null &&
                wo.DueDate.Value < today &&
                (wo.Status == WorkOrderStatus.Draft ||
                 wo.Status == WorkOrderStatus.Assigned ||
                 wo.Status == WorkOrderStatus.InProgress));
        }

        return query;
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
