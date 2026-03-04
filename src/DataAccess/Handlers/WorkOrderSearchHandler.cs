using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers
{
    public class WorkOrderSearchHandler(DataContext context) : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
    {
        public async Task<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
            CancellationToken cancellationToken = default)
        {
            IQueryable<WorkOrder> query = context.Set<WorkOrder>();

            if (specification.Assignee != null)
            {
                query = query.Where(wo => wo.Assignee == specification.Assignee);
            }

            if (specification.Creator != null)
            {
                query = query.Where(wo => wo.Creator == specification.Creator);
            }

            if (specification.Status != null)
            {
                query = query.Where(wo => wo.Status == specification.Status);
            }

            if (!string.IsNullOrWhiteSpace(specification.Building))
            {
                query = query.Where(wo => wo.Building == specification.Building);
            }

            if (!string.IsNullOrWhiteSpace(specification.Floor))
            {
                query = query.Where(wo => wo.Floor == specification.Floor);
            }

            return await query.ToArrayAsync(cancellationToken);
        }
    }
}