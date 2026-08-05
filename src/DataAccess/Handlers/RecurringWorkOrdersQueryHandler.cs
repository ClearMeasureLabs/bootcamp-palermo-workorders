using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Handler for finding recurring work orders that are due for generation.
/// </summary>
public class RecurringWorkOrdersQueryHandler(DataContext context) 
    : IRequestHandler<RecurringWorkOrdersQuery, RecurringWorkOrdersQueryResult>
{
    public async Task<RecurringWorkOrdersQueryResult> Handle(
        RecurringWorkOrdersQuery request, 
        CancellationToken cancellationToken)
    {
        var dueWorkOrders = await context.Set<Core.Model.WorkOrder>()
            .Where(x => x.IsRecurring 
                && x.NextScheduledDate.HasValue 
                && x.NextScheduledDate.Value <= request.AsOfDate)
            .ToArrayAsync(cancellationToken);
            
        return new RecurringWorkOrdersQueryResult 
        { 
            DueWorkOrders = dueWorkOrders
        };
    }
}
