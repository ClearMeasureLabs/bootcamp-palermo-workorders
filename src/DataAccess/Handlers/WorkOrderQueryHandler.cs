using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderByNumberQuery, WorkOrder?>
{
    public async Task<WorkOrder?> GetWorkOrderAsync(string number,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == number, cancellationToken);
    }

    public async Task<WorkOrder[]> GetWorkOrdersAsync(WorkOrderSearchSpecification specification,
        CancellationToken cancellationToken = default)
    {
        var query = WorkOrderQueryFilters.Apply(context.Set<WorkOrder>(), specification);
        return await query.ToArrayAsync(cancellationToken);
    }

    public async Task<WorkOrder?> Handle(WorkOrderByNumberQuery request,
        CancellationToken cancellationToken = default)
    {
        return await GetWorkOrderAsync(request.Number, cancellationToken);
    }
}