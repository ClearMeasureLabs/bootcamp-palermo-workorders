package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.EmployeeSessionService;
import com.clearmeasure.workorders.application.WorkOrderAttachmentService;
import com.clearmeasure.workorders.application.WorkOrderService;
import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.domain.WorkOrder;
import jakarta.servlet.http.HttpSession;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
public class WorkOrderManagePageController {
    private final WorkOrderService workOrders;
    private final WorkOrderAttachmentService attachments;
    private final EmployeeSessionService sessions;

    public WorkOrderManagePageController(WorkOrderService workOrders, WorkOrderAttachmentService attachments,
                                         EmployeeSessionService sessions) {
        this.workOrders = workOrders;
        this.attachments = attachments;
        this.sessions = sessions;
    }

    @GetMapping("/work-orders/{number}/manage")
    public String manage(@PathVariable String number, Model model, HttpSession session) {
        Employee actor = sessions.currentOrNull(session);
        if (actor == null) return "redirect:/login";
        WorkOrder workOrder = workOrders.get(number);
        model.addAttribute("workOrder", workOrder);
        model.addAttribute("attachments", attachments.list(number, actor));
        model.addAttribute("currentUser", actor);
        return "work-order-manage";
    }

    @PostMapping("/work-orders/{number}/manage/attachments")
    public String add(@PathVariable String number, @RequestParam String fileName,
                      @RequestParam String contentType, @RequestParam long fileSize, HttpSession session) {
        Employee actor = sessions.currentOrNull(session);
        if (actor == null) return "redirect:/login";
        attachments.add(number, fileName, contentType, fileSize, actor);
        return "redirect:/work-orders/" + number + "/manage";
    }
}
