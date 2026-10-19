using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WeeklyCompletedSummaryQueryHandler(DataContext context) :
    IRequestHandler<WeeklyCompletedSummaryQuery, int>
{
    public async Task<int> Handle(WeeklyCompletedSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        return await CountCompletedSinceAsync(weekAgo, cancellationToken);
    }

    private async Task<int> CountCompletedSinceAsync(DateTime since, CancellationToken cancellationToken)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(
                wo => wo.CompletedDate != null && wo.CompletedDate >= since,
                cancellationToken);
    }
}
