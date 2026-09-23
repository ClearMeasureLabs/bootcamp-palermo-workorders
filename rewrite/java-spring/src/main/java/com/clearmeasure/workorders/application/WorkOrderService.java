package com.clearmeasure.workorders.application;

import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import com.clearmeasure.workorders.persistence.WorkOrderRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.Clock;
import java.time.LocalDate;
import java.util.List;

@Service
@Transactional
public class WorkOrderService {
    private final WorkOrderRepository repository;
    private final Clock clock;
    public WorkOrderService(WorkOrderRepository repository, Clock clock) { this.repository = repository; this.clock = clock; }

    public WorkOrder create(String title, String description, String instructions, String room, String creator, LocalDate dueDate) {
        if (title == null || title.isBlank()) throw new IllegalArgumentException("Title is required");
        String number = "WO-" + System.currentTimeMillis();
        return repository.save(new WorkOrder(number, title.trim(), description, instructions, room, creator, dueDate));
    }
    @Transactional(readOnly = true)
    public List<WorkOrder> list(WorkOrderStatus status, boolean overdueOnly) {
        return repository.findMatching(status, overdueOnly, LocalDate.now(clock),
            List.of(WorkOrderStatus.DRAFT, WorkOrderStatus.ASSIGNED, WorkOrderStatus.IN_PROGRESS));
    }
    @Transactional(readOnly = true)
    public WorkOrder get(String number) { return repository.findByNumber(number).orElseThrow(() -> new WorkOrderNotFoundException(number)); }
    public WorkOrder assign(String number, String assignee) { WorkOrder order = get(number); order.assign(assignee); return order; }
    public WorkOrder begin(String number) { WorkOrder order = get(number); order.begin(); return order; }
    public WorkOrder complete(String number) { WorkOrder order = get(number); order.complete(); return order; }
    public WorkOrder cancel(String number) { WorkOrder order = get(number); order.cancel(); return order; }
}
