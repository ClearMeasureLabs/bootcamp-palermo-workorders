package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderService;
import com.clearmeasure.workorders.application.EmployeeSessionService;
import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import jakarta.servlet.http.HttpSession;
import jakarta.servlet.http.HttpServletRequest;
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

    @GetMapping({"/work-orders/new", "/workorder/manage"})
    public String createPage(Model model, HttpSession session, HttpServletRequest request) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        if (!currentUser.canCreateWorkOrder()) return "redirect:/";
        model.addAttribute("currentUser", currentUser);
        model.addAttribute("csrfToken", request.getAttribute(CsrfProtectionFilter.REQUEST_ATTRIBUTE));
        return "work-order-new";
    }

    @GetMapping("/counter")
    public String counter(Model model, HttpSession session, HttpServletRequest request) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        model.addAttribute("currentUser", currentUser);
        model.addAttribute("csrfToken", request.getAttribute(CsrfProtectionFilter.REQUEST_ATTRIBUTE));
        model.addAttribute("count", session.getAttribute("activityCounter") instanceof Integer count ? count : 0);
        return "counter";
    }

    @PostMapping("/counter/{action}")
    public String updateCounter(@PathVariable String action, HttpSession session) {
        if (sessions.currentOrNull(session) == null) return "redirect:/login";
        int count = session.getAttribute("activityCounter") instanceof Integer current ? current : 0;
        switch (action) {
            case "increment" -> session.setAttribute("activityCounter", count + 1);
            case "reset" -> session.setAttribute("activityCounter", 0);
            default -> throw new IllegalArgumentException("Unknown counter action");
        }
        return "redirect:/counter";
    }

    @GetMapping("/") public String home(Model model, HttpSession session,
        HttpServletRequest request) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        model.addAttribute("currentUser", currentUser);
        model.addAttribute("csrfToken", request.getAttribute(CsrfProtectionFilter.REQUEST_ATTRIBUTE));
        model.addAttribute("statusCounts", service.statusCounts());
        return "work-order-home";
    }

    @GetMapping("/workorder/search") public String search(@RequestParam(required=false) WorkOrderStatus status,
        @RequestParam(defaultValue = "false") boolean overdueOnly,
        @RequestParam(required = false) String creator,
        @RequestParam(required = false) String assignee,
        @RequestParam(defaultValue = "false") boolean assignedToMe,
        @RequestParam(defaultValue = "false") boolean allAssigned, Model model, HttpSession session,
        HttpServletRequest request) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        if (assignedToMe) assignee = currentUser.getUsername();
        model.addAttribute("orders", service.list(status, overdueOnly, creator, assignee, allAssigned));
        model.addAttribute("statuses", WorkOrderStatus.values());
        model.addAttribute("employees", sessions.employeeChoices());
        model.addAttribute("selectedStatus", status); model.addAttribute("overdueOnly", overdueOnly);
        model.addAttribute("selectedCreator", creator); model.addAttribute("selectedAssignee", assignee);
        model.addAttribute("assignedToMe", assignedToMe);
        model.addAttribute("allAssigned", allAssigned);
        model.addAttribute("currentUser", currentUser);
        model.addAttribute("csrfToken", request.getAttribute(CsrfProtectionFilter.REQUEST_ATTRIBUTE));
        model.addAttribute("fulfillmentEmployees", sessions.fulfillmentChoices());
        return "work-orders";
    }
    @PostMapping("/work-orders") public String create(@RequestParam String title, @RequestParam(required=false) String description,
        @RequestParam(required=false) String instructions,
        @RequestParam(required=false) String roomNumber,
        @RequestParam(required=false) LocalDate dueDate, HttpSession session) {
        Employee currentUser = sessions.currentOrNull(session);
        if (currentUser == null) return "redirect:/login";
        service.create(title, description, instructions, roomNumber, currentUser, dueDate); return "redirect:/workorder/search";
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
        return "redirect:/workorder/search";
    }
}
