package com.clearmeasure.workorders.application;

import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import com.clearmeasure.workorders.persistence.WorkOrderRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.LocalDate;
import java.util.List;

@Service
@Transactional
public class WorkOrderService {
    private final WorkOrderRepository repository;
    public WorkOrderService(WorkOrderRepository repository) { this.repository = repository; }

    public WorkOrder create(String title, String description, String room, String creator, LocalDate dueDate) {
        if (title == null || title.isBlank()) throw new IllegalArgumentException("Title is required");
        String number = "WO-" + System.currentTimeMillis();
        return repository.save(new WorkOrder(number, title.trim(), description, room, creator, dueDate));
    }
    @Transactional(readOnly = true)
    public List<WorkOrder> list(WorkOrderStatus status) {
        return status == null ? repository.findAllByOrderByCreatedAtDesc() : repository.findByStatusOrderByCreatedAtDesc(status);
    }
    @Transactional(readOnly = true)
    public WorkOrder get(String number) { return repository.findByNumber(number).orElseThrow(() -> new WorkOrderNotFoundException(number)); }
    public WorkOrder assign(String number, String assignee) { WorkOrder order = get(number); order.assign(assignee); return order; }
    public WorkOrder begin(String number) { WorkOrder order = get(number); order.begin(); return order; }
    public WorkOrder complete(String number) { WorkOrder order = get(number); order.complete(); return order; }
    public WorkOrder cancel(String number) { WorkOrder order = get(number); order.cancel(); return order; }
}
