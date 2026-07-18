using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkRequestQueryHandler(DataContext context) :
    IRequestHandler<WorkRequestByNumberQuery, WorkRequest?>
{
    public async Task<WorkRequest?> GetWorkRequestAsync(string number)
    {
        return await context.Set<WorkRequest>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == number);
    }

    public async Task<WorkRequest[]> GetWorkRequestsAsync(WorkRequestSearchSpecification specification)
    {
        IQueryable<WorkRequest> query = context.Set<WorkRequest>();

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

    public async Task<WorkRequest?> Handle(WorkRequestByNumberQuery request,
        CancellationToken cancellationToken = default)
    {
        return await GetWorkRequestAsync(request.Number);
    }
}