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
            var query = WorkOrderQueryFilters.Apply(context.Set<WorkOrder>(), specification);
            return await query.ToArrayAsync(cancellationToken);
        }
    }
}