package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderService;
import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;
import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/api/work-orders")
public class WorkOrderController {
    private final WorkOrderService service;
    public WorkOrderController(WorkOrderService service) { this.service = service; }

    @GetMapping public List<WorkOrder> list(@RequestParam(required = false) WorkOrderStatus status) { return service.list(status); }
    @GetMapping("/{number}") public WorkOrder get(@PathVariable String number) { return service.get(number); }
    @PostMapping @ResponseStatus(HttpStatus.CREATED)
    public WorkOrder create(@Valid @RequestBody CreateWorkOrder request) {
        return service.create(request.title(), request.description(), request.roomNumber(), request.creatorName(), request.dueDate());
    }
    @PostMapping("/{number}/assign") public WorkOrder assign(@PathVariable String number, @Valid @RequestBody Assignee request) { return service.assign(number, request.assignee()); }
    @PostMapping("/{number}/begin") public WorkOrder begin(@PathVariable String number) { return service.begin(number); }
    @PostMapping("/{number}/complete") public WorkOrder complete(@PathVariable String number) { return service.complete(number); }
    @PostMapping("/{number}/cancel") public WorkOrder cancel(@PathVariable String number) { return service.cancel(number); }

    public record CreateWorkOrder(@NotBlank @Size(max=240) String title, @Size(max=4000) String description,
                                  @Size(max=900) String roomNumber, @Size(max=160) String creatorName, LocalDate dueDate) { }
    public record Assignee(@NotBlank @Size(max=160) String assignee) { }
}
