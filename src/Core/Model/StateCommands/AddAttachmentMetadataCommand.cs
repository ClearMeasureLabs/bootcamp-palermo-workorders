using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AddAttachmentMetadataCommand(
    WorkOrder WorkOrder,
    Employee UploadedBy,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<WorkOrderAttachment>
{
    public WorkOrderAttachment CreateAttachment(DateTime uploadedDate)
    {
        if (string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException("FileName is required.", nameof(FileName));

        return new WorkOrderAttachment
        {
            Id = Guid.NewGuid(),
            WorkOrderId = WorkOrder.Id,
            FileName = FileName,
            ContentType = ContentType,
            FileSize = FileSize,
            UploadedById = UploadedBy.Id,
            UploadedDate = uploadedDate
        };
    }
}
