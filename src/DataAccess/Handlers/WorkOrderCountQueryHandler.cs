using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderCountQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountQuery, int>
{
    public async Task<int> Handle(WorkOrderCountQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }
}
