using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AddAttachmentMetadataCommand(
    WorkOrder WorkOrder,
    Employee UploadedBy,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<WorkOrderAttachment>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> BlockedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".exe", ".bat", ".cmd", ".ps1" };

    public WorkOrderAttachment CreateAttachment(DateTime uploadedDate)
    {
        if (string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException("FileName is required.", nameof(FileName));

        if (FileSize > MaxFileSizeBytes)
            throw new ArgumentException("FileSize must not exceed 10 MB.", nameof(FileSize));

        var extension = Path.GetExtension(FileName);
        if (BlockedExtensions.Contains(extension))
            throw new ArgumentException($"File extension '{extension}' is not allowed.", nameof(FileName));

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
