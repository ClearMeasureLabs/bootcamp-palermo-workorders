package com.clearmeasure.workorders.application;

import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderAttachment;
import com.clearmeasure.workorders.persistence.EmployeeRepository;
import com.clearmeasure.workorders.persistence.WorkOrderAttachmentRepository;
import com.clearmeasure.workorders.persistence.WorkOrderRepository;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.function.Function;
import java.util.stream.Collectors;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@Transactional
public class WorkOrderAttachmentService {
    private final WorkOrderAttachmentRepository attachments;
    private final WorkOrderRepository workOrders;
    private final EmployeeRepository employees;
    private final Clock clock;

    public WorkOrderAttachmentService(WorkOrderAttachmentRepository attachments,
                                      WorkOrderRepository workOrders,
                                      EmployeeRepository employees, Clock clock) {
        this.attachments = attachments;
        this.workOrders = workOrders;
        this.employees = employees;
        this.clock = clock;
    }

    public AttachmentView add(String number, String fileName, String contentType, long fileSize, Employee actor) {
        WorkOrder workOrder = findWorkOrder(number);
        requireParticipant(workOrder, actor);
        if (fileName == null || fileName.isBlank()) throw new IllegalArgumentException("File name is required");
        if (fileName.length() > 500) throw new IllegalArgumentException("File name must be 500 characters or fewer");
        if (contentType == null) throw new IllegalArgumentException("Content type is required");
        if (contentType.length() > 200) throw new IllegalArgumentException("Content type must be 200 characters or fewer");
        WorkOrderAttachment attachment = attachments.save(new WorkOrderAttachment(UUID.randomUUID(), workOrder.getId(),
            fileName, contentType, fileSize, actor.getId(), OffsetDateTime.ofInstant(clock.instant(), ZoneOffset.UTC)));
        return view(attachment, actor.getDisplayName());
    }

    @Transactional(readOnly = true)
    public List<AttachmentView> list(String number, Employee actor) {
        WorkOrder workOrder = findWorkOrder(number);
        requireParticipant(workOrder, actor);
        List<WorkOrderAttachment> items = attachments.findByWorkOrderIdOrderByUploadedDateAsc(workOrder.getId());
        Map<UUID, Employee> uploaders = employees.findAllById(
            items.stream().map(WorkOrderAttachment::getUploadedById).distinct().toList()).stream()
            .collect(Collectors.toMap(Employee::getId, Function.identity()));
        return items.stream().map(item -> view(item, uploaders.get(item.getUploadedById()).getDisplayName())).toList();
    }

    private WorkOrder findWorkOrder(String number) {
        return workOrders.findByNumber(number).orElseThrow(() -> new WorkOrderNotFoundException(number));
    }

    private static void requireParticipant(WorkOrder workOrder, Employee actor) {
        if (!actor.getUsername().equals(workOrder.getCreatorUsername())
            && !actor.getUsername().equals(workOrder.getAssigneeUsername())) {
            throw new ForbiddenActionException("Only the creator or assigned employee can manage attachment metadata");
        }
    }

    private static AttachmentView view(WorkOrderAttachment attachment, String uploadedByName) {
        return new AttachmentView(attachment.getId(), attachment.getWorkOrderId(), attachment.getFileName(),
            attachment.getContentType(), attachment.getFileSize(), attachment.getUploadedById(),
            attachment.getUploadedDate(), uploadedByName);
    }

    public record AttachmentView(UUID id, UUID workOrderId, String fileName, String contentType, long fileSize,
                                 UUID uploadedById, OffsetDateTime uploadedDate, String uploadedByName) { }
}
