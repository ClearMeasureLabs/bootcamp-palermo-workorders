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
    public async Task<WorkOrder?> GetWorkOrderAsync(string number)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == number);
    }

    public async Task<WorkOrder[]> GetWorkOrdersAsync(WorkOrderSearchSpecification specification)
    {
        var query = WorkOrderQueryFilters.Apply(context.Set<WorkOrder>(), specification);
        return await query.ToArrayAsync();
    }

    public async Task<WorkOrder?> Handle(WorkOrderByNumberQuery request,
        CancellationToken cancellationToken = default)
    {
        return await GetWorkOrderAsync(request.Number);
    }
}