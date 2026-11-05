package com.clearmeasure.workorders.web;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class CsrfTokenController {
    @GetMapping("/api/csrf")
    public CsrfToken csrf(HttpServletRequest request) {
        return new CsrfToken((String) request.getAttribute(CsrfProtectionFilter.REQUEST_ATTRIBUTE));
    }

    public record CsrfToken(String token) { }
}
