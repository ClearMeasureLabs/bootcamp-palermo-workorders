package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderAttachmentService;
import com.clearmeasure.workorders.application.WorkOrderAttachmentService.AttachmentView;
import com.clearmeasure.workorders.application.EmployeeSessionService;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import jakarta.validation.constraints.PositiveOrZero;
import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/work-orders/{number}/attachments")
public class WorkOrderAttachmentController {
    private final WorkOrderAttachmentService attachments;
    private final EmployeeSessionService sessions;

    public WorkOrderAttachmentController(WorkOrderAttachmentService attachments, EmployeeSessionService sessions) {
        this.attachments = attachments;
        this.sessions = sessions;
    }

    @GetMapping
    public List<AttachmentView> list(@PathVariable String number, HttpSession session) {
        return attachments.list(number, sessions.requireCurrent(session));
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public AttachmentView add(@PathVariable String number, @Valid @RequestBody AddAttachmentRequest request,
                              HttpSession session) {
        return attachments.add(number, request.fileName(), request.contentType(), request.fileSize(),
            sessions.requireCurrent(session));
    }

    public record AddAttachmentRequest(@NotBlank @Size(max = 500) String fileName,
                                       @NotBlank @Size(max = 200) String contentType,
                                       @PositiveOrZero long fileSize) { }
}
