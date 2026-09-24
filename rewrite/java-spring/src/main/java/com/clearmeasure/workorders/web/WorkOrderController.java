package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderService;
import com.clearmeasure.workorders.application.EmployeeSessionService;
import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;
import java.time.LocalDate;
import java.util.List;
import jakarta.servlet.http.HttpSession;

@RestController
@RequestMapping("/api/work-orders")
public class WorkOrderController {
    private final WorkOrderService service;
    private final EmployeeSessionService sessions;
    public WorkOrderController(WorkOrderService service, EmployeeSessionService sessions) {
        this.service = service;
        this.sessions = sessions;
    }

    @GetMapping public List<WorkOrder> list(@RequestParam(required = false) WorkOrderStatus status,
                                            @RequestParam(defaultValue = "false") boolean overdueOnly,
                                            HttpSession session) {
        sessions.requireCurrent(session);
        return service.list(status, overdueOnly);
    }
    @GetMapping("/{number}") public WorkOrder get(@PathVariable String number, HttpSession session) {
        sessions.requireCurrent(session);
        return service.get(number);
    }
    @PostMapping @ResponseStatus(HttpStatus.CREATED)
    public WorkOrder create(@Valid @RequestBody CreateWorkOrder request, HttpSession session) {
        Employee actor = sessions.requireCurrent(session);
        return service.create(request.title(), request.description(), request.instructions(), request.roomNumber(), actor, request.dueDate());
    }
    @PostMapping("/{number}/assign") public WorkOrder assign(@PathVariable String number, @Valid @RequestBody Assignee request, HttpSession session) {
        return service.assign(number, request.assignee(), sessions.requireCurrent(session));
    }
    @PostMapping("/{number}/begin") public WorkOrder begin(@PathVariable String number, HttpSession session) {
        return service.begin(number, sessions.requireCurrent(session));
    }
    @PostMapping("/{number}/shelve") public WorkOrder shelve(@PathVariable String number, HttpSession session) {
        return service.shelve(number, sessions.requireCurrent(session));
    }
    @PostMapping("/{number}/complete") public WorkOrder complete(@PathVariable String number, HttpSession session) {
        return service.complete(number, sessions.requireCurrent(session));
    }
    @PostMapping("/{number}/cancel") public WorkOrder cancel(@PathVariable String number, HttpSession session) {
        return service.cancel(number, sessions.requireCurrent(session));
    }

    public record CreateWorkOrder(@NotBlank @Size(max=240) String title, @Size(max=4000) String description,
                                  @Size(max=4000) String instructions,
                                  @Size(max=900) String roomNumber, @Size(max=160) String creatorName, LocalDate dueDate) { }
    public record Assignee(@NotBlank @Size(max=160) String assignee) { }
}
