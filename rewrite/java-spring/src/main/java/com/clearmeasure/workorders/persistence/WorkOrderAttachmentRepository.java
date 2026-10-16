package com.clearmeasure.workorders.persistence;

import com.clearmeasure.workorders.domain.WorkOrderAttachment;
import java.util.List;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface WorkOrderAttachmentRepository extends JpaRepository<WorkOrderAttachment, UUID> {
    List<WorkOrderAttachment> findByWorkOrderIdOrderByUploadedDateAsc(UUID workOrderId);
}
