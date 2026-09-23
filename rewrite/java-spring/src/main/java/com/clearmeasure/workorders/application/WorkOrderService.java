package com.clearmeasure.workorders.application;

import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.persistence.EmployeeRepository;
import com.clearmeasure.workorders.persistence.WorkOrderRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.Clock;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

@Service
@Transactional
public class WorkOrderService {
    private final WorkOrderRepository repository;
    private final EmployeeRepository employees;
    private final Clock clock;
    public WorkOrderService(WorkOrderRepository repository, EmployeeRepository employees, Clock clock) {
        this.repository = repository;
        this.employees = employees;
        this.clock = clock;
    }

    public WorkOrder create(String title, String description, String instructions, String room, Employee creator, LocalDate dueDate) {
        if (title == null || title.isBlank()) throw new IllegalArgumentException("Title is required");
        if (!creator.canCreateWorkOrder()) throw new ForbiddenActionException("Your employee roles cannot create work orders");
        String number = "WO-" + UUID.randomUUID().toString().replace("-", "").substring(0, 16).toUpperCase();
        return repository.save(new WorkOrder(number, title.trim(), description, instructions, room,
            creator.getDisplayName(), creator.getUsername(), dueDate));
    }
    @Transactional(readOnly = true)
    public List<WorkOrder> list(WorkOrderStatus status, boolean overdueOnly) {
        return repository.findMatching(status, overdueOnly, LocalDate.now(clock),
            List.of(WorkOrderStatus.DRAFT, WorkOrderStatus.ASSIGNED, WorkOrderStatus.IN_PROGRESS));
    }
    @Transactional(readOnly = true)
    public WorkOrder get(String number) { return repository.findByNumber(number).orElseThrow(() -> new WorkOrderNotFoundException(number)); }
    public WorkOrder assign(String number, String assigneeUsername, Employee actor) {
        WorkOrder order = get(number);
        requireCreator(order, actor);
        Employee assignee = employees.findByUsername(assigneeUsername).orElseThrow(
            () -> new IllegalArgumentException("Select a valid employee"));
        if (!assignee.canFulfillWorkOrder())
            throw new IllegalArgumentException("The selected employee cannot fulfill work orders");
        order.assign(assignee);
        return order;
    }
    public WorkOrder begin(String number, Employee actor) {
        WorkOrder order = get(number);
        requireAssignee(order, actor);
        order.begin(actor.getUsername());
        return order;
    }
    public WorkOrder shelve(String number, Employee actor) {
        WorkOrder order = get(number);
        requireAssignee(order, actor);
        order.shelve(actor.getUsername());
        return order;
    }
    public WorkOrder complete(String number, Employee actor) {
        WorkOrder order = get(number);
        requireAssignee(order, actor);
        order.complete(actor.getUsername());
        return order;
    }
    public WorkOrder cancel(String number, Employee actor) {
        WorkOrder order = get(number);
        requireCreator(order, actor);
        order.cancel(actor.getUsername());
        return order;
    }

    private static void requireCreator(WorkOrder order, Employee actor) {
        if (!actor.getUsername().equals(order.getCreatorUsername()))
            throw new ForbiddenActionException("Only the work order creator can perform this action");
    }
    private static void requireAssignee(WorkOrder order, Employee actor) {
        if (!actor.getUsername().equals(order.getAssigneeUsername()))
            throw new ForbiddenActionException("Only the assigned employee can perform this action");
    }
}
