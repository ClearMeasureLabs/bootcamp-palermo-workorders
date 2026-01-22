using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class AuditEntryQueryHandler(DbContext dbContext) : IRequestHandler<AuditEntriesByWorkOrderIdQuery, AuditEntry[]>
{
    public async Task<AuditEntry[]> Handle(AuditEntriesByWorkOrderIdQuery request, CancellationToken cancellationToken)
    {
        var auditEntries = await dbContext.Set<AuditEntry>()
            .Where(a => a.WorkOrderId == request.WorkOrderId)
            .OrderBy(a => a.Sequence)
            .ToArrayAsync(cancellationToken);

        return auditEntries;
    }
}
