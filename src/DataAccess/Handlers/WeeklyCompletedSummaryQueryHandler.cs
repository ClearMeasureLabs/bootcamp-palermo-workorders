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
        var completedDates = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Select(wo => wo.CompletedDate)
            .ToListAsync(cancellationToken);
        return CountCompletedSince(completedDates, weekAgo);
    }

    /// <summary>
    /// Counts completed dates on or after <paramref name="since"/>.
    /// </summary>
    public static int CountCompletedSince(IEnumerable<DateTime?> completedDates, DateTime since)
    {
        var count = 0;
        foreach (var completedDate in completedDates)
        {
            if (completedDate != null && completedDate >= since)
                count++;
        }

        return count;
    }
}
