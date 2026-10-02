package com.clearmeasure.workorders.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

/** Attachment metadata only; binary content is not uploaded or stored by this prototype. */
@Entity
@Table(name = "work_order_attachments")
public class WorkOrderAttachment {
    @Id
    private UUID id;

    @Column(name = "work_order_id", nullable = false)
    private UUID workOrderId;

    @Column(name = "file_name", nullable = false, length = 500)
    private String fileName;

    @Column(name = "content_type", nullable = false, length = 200)
    private String contentType;

    @Column(name = "file_size", nullable = false)
    private long fileSize;

    @Column(name = "uploaded_by_id", nullable = false)
    private UUID uploadedById;

    @Column(name = "uploaded_date", nullable = false)
    private OffsetDateTime uploadedDate;

    protected WorkOrderAttachment() { }

    public WorkOrderAttachment(UUID id, UUID workOrderId, String fileName, String contentType,
                               long fileSize, UUID uploadedById, OffsetDateTime uploadedDate) {
        this.id = id;
        this.workOrderId = workOrderId;
        this.fileName = fileName;
        this.contentType = contentType;
        this.fileSize = fileSize;
        this.uploadedById = uploadedById;
        this.uploadedDate = uploadedDate;
    }

    public UUID getId() { return id; }
    public UUID getWorkOrderId() { return workOrderId; }
    public String getFileName() { return fileName; }
    public String getContentType() { return contentType; }
    public long getFileSize() { return fileSize; }
    public UUID getUploadedById() { return uploadedById; }
    public OffsetDateTime getUploadedDate() { return uploadedDate; }
}
