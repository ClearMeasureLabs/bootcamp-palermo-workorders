using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

internal static class WorkOrderQueryFilters
{
    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        Employee? assignee,
        Employee? creator,
        WorkOrderStatus? status)
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

        return query;
    }

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSearchSpecification specification) =>
        Apply(query, specification.Assignee, specification.Creator, specification.Status);

    public static IQueryable<WorkOrder> Apply(
        IQueryable<WorkOrder> query,
        WorkOrderSpecificationQuery specification) =>
        Apply(query, specification.Assignee, specification.Creator, specification.Status);
}
