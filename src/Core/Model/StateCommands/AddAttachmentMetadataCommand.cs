using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AddAttachmentMetadataCommand(
    WorkRequest WorkRequest,
    Employee UploadedBy,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<WorkRequestAttachment>
{
    public WorkRequestAttachment CreateAttachment(DateTime uploadedDate)
    {
        if (string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException("FileName is required.", nameof(FileName));

        return new WorkRequestAttachment
        {
            Id = Guid.NewGuid(),
            WorkRequestId = WorkRequest.Id,
            FileName = FileName,
            ContentType = ContentType,
            FileSize = FileSize,
            UploadedById = UploadedBy.Id,
            UploadedDate = uploadedDate
        };
    }
}
