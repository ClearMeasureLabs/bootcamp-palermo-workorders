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
            .SingleOrDefaultAsync(wo => wo.Number == number);
    }

    public async Task<WorkOrder[]> GetWorkOrdersAsync(WorkOrderSearchSpecification specification)
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

        return await query.ToArrayAsync();
    }

    public async Task<WorkOrder?> Handle(WorkOrderByNumberQuery request,
        CancellationToken cancellationToken = default)
    {
        return await GetWorkOrderAsync(request.Number);
    }
}