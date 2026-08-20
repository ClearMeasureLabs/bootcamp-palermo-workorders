using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class AddAttachmentMetadataCommandHandler(DbContext dbContext, TimeProvider time)
    : IRequestHandler<AddAttachmentMetadataCommand, WorkOrderAttachment>
{
    public async Task<WorkOrderAttachment> Handle(AddAttachmentMetadataCommand request,
        CancellationToken cancellationToken = default)
    {
        var attachment = request.CreateAttachment(time.GetUtcNow().DateTime);
        dbContext.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return attachment;
    }
}
