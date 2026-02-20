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
            IQueryable<WorkOrder> query = context.Set<WorkOrder>()
                .Include(wo => wo.Rooms);

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

            if (specification.Room != null)
            {
                query = query.Where(wo => wo.Rooms.Any(r => r.Id == specification.Room.Id));
            }

            return await query.ToArrayAsync(cancellationToken);
        }
    }
}