using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderAttachmentsQueryHandler(DataContext context)
    : IRequestHandler<WorkOrderAttachmentsQuery, WorkOrderAttachment[]>
{
    public async Task<WorkOrderAttachment[]> Handle(WorkOrderAttachmentsQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrderAttachment>()
            .Include(a => a.UploadedBy)
            .Where(a => a.WorkOrderId == request.WorkOrderId)
            .OrderBy(a => a.UploadedDate)
            .ToArrayAsync(cancellationToken);
    }
}
