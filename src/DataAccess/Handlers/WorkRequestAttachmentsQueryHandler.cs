using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkRequestAttachmentsQueryHandler(DataContext context)
    : IRequestHandler<WorkRequestAttachmentsQuery, WorkRequestAttachment[]>
{
    public async Task<WorkRequestAttachment[]> Handle(WorkRequestAttachmentsQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkRequestAttachment>()
            .Include(a => a.UploadedBy)
            .Where(a => a.WorkRequestId == request.WorkRequestId)
            .OrderBy(a => a.UploadedDate)
            .ToArrayAsync(cancellationToken);
    }
}
