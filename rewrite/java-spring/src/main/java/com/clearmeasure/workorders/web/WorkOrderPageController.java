package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderService;
import com.clearmeasure.workorders.application.EmployeeSessionService;
import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import jakarta.servlet.http.HttpSession;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.*;
import java.time.LocalDate;

@Controller
public class WorkOrderPageController {
    private final WorkOrderService service;
    private final EmployeeSessionService sessions;
    public WorkOrderPageController(WorkOrderService service, EmployeeSessionService sessions) {
        this.service = service;
        this.sessions = sessions;
    }

    @GetMapping("/") public String home(@RequestParam(required=false) WorkOrderStatus status,
        @RequestParam(defaultValue = "false") boolean overdueOnly, Model model, HttpSession session) {
        model.addAttribute("orders", service.list(status, overdueOnly)); model.addAttribute("statuses", WorkOrderStatus.values());
        model.addAttribute("selectedStatus", status); model.addAttribute("overdueOnly", overdueOnly);
        Employee currentUser = sessions.currentOrNull(session);
        model.addAttribute("currentUser", currentUser);
        model.addAttribute("fulfillmentEmployees", sessions.fulfillmentChoices());
        return "work-orders";
    }
    @PostMapping("/work-orders") public String create(@RequestParam String title, @RequestParam(required=false) String description,
        @RequestParam(required=false) String instructions,
        @RequestParam(required=false) String roomNumber,
        @RequestParam(required=false) LocalDate dueDate, HttpSession session) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        service.create(title, description, instructions, roomNumber, currentUser, dueDate); return "redirect:/";
    }
    @PostMapping("/work-orders/{number}/{action}") public String action(@PathVariable String number, @PathVariable String action,
        @RequestParam(required=false) String assignee, HttpSession session) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        switch (action) {
            case "assign" -> service.assign(number, assignee, currentUser);
            case "begin" -> service.begin(number, currentUser);
            case "shelve" -> service.shelve(number, currentUser);
            case "complete" -> service.complete(number, currentUser);
            case "cancel" -> service.cancel(number, currentUser);
            default -> throw new IllegalArgumentException("Unknown action");
        }
        return "redirect:/";
    }
}
