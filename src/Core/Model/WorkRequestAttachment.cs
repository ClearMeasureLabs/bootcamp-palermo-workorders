namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkRequestAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime UploadedDate { get; set; }

    public Employee? UploadedBy { get; set; }
}
