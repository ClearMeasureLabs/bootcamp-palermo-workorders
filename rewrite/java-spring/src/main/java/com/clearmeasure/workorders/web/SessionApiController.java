package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.EmployeeSessionService;
import com.clearmeasure.workorders.application.EmployeeSessionService.EmployeeView;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.HttpStatus;
import org.springframework.context.annotation.Profile;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/session")
@Profile({"dev", "test"})
public class SessionApiController {
    private final EmployeeSessionService sessions;
    public SessionApiController(EmployeeSessionService sessions) { this.sessions = sessions; }

    @PostMapping public EmployeeView login(@RequestBody LoginRequest request, HttpServletRequest httpRequest) {
        return sessions.login(request.username(), httpRequest);
    }
    @GetMapping public EmployeeView current(HttpSession session) { return sessions.currentView(session); }
    @DeleteMapping @ResponseStatus(HttpStatus.NO_CONTENT)
    public void logout(HttpSession session) { sessions.logout(session); }

    public record LoginRequest(String username) { }
}
