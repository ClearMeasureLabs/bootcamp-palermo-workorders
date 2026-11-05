package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.EmployeeSessionService;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.stereotype.Controller;
import org.springframework.context.annotation.Profile;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.servlet.mvc.support.RedirectAttributes;

@Controller
@Profile({"dev", "test"})
public class LoginPageController {
    private final EmployeeSessionService sessions;
    public LoginPageController(EmployeeSessionService sessions) { this.sessions = sessions; }

    @GetMapping("/login")
    public String login(@RequestParam(required = false) String error, Model model, HttpServletRequest request) {
        model.addAttribute("employees", sessions.employeeChoices());
        model.addAttribute("error", error);
        model.addAttribute("csrfToken", request.getAttribute(CsrfProtectionFilter.REQUEST_ATTRIBUTE));
        return "login";
    }

    @PostMapping("/login")
    public String login(@RequestParam String username, HttpServletRequest request, RedirectAttributes redirect) {
        try {
            sessions.login(username, request);
            return "redirect:/";
        } catch (IllegalArgumentException exception) {
            redirect.addAttribute("error", exception.getMessage());
            return "redirect:/login";
        }
    }

    @PostMapping("/login/lovejoy")
    public String lovejoy(HttpServletRequest request, RedirectAttributes redirect) {
        return login("tlovejoy", request, redirect);
    }

    @PostMapping("/logout")
    public String logout(jakarta.servlet.http.HttpSession session) {
        sessions.logout(session);
        return "redirect:/login";
    }
}
