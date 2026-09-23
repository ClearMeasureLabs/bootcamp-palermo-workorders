package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderService;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.*;
import java.time.LocalDate;

@Controller
public class WorkOrderPageController {
    private final WorkOrderService service;
    public WorkOrderPageController(WorkOrderService service) { this.service = service; }

    @GetMapping("/") public String home(@RequestParam(required=false) WorkOrderStatus status, Model model) {
        model.addAttribute("orders", service.list(status)); model.addAttribute("statuses", WorkOrderStatus.values()); model.addAttribute("selectedStatus", status);
        return "work-orders";
    }
    @PostMapping("/work-orders") public String create(@RequestParam String title, @RequestParam(required=false) String description,
        @RequestParam(required=false) String roomNumber, @RequestParam(required=false) String creatorName,
        @RequestParam(required=false) LocalDate dueDate) {
        service.create(title, description, roomNumber, creatorName, dueDate); return "redirect:/";
    }
    @PostMapping("/work-orders/{number}/{action}") public String action(@PathVariable String number, @PathVariable String action,
        @RequestParam(required=false) String assignee) {
        switch (action) { case "assign" -> service.assign(number, assignee); case "begin" -> service.begin(number); case "complete" -> service.complete(number); case "cancel" -> service.cancel(number); default -> throw new IllegalArgumentException("Unknown action"); }
        return "redirect:/";
    }
}
