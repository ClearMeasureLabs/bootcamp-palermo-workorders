package com.clearmeasure.workorders.web;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.UUID;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/** Session-bound synchronizer token for every state-changing browser and API request. */
@Component
public class CsrfProtectionFilter extends OncePerRequestFilter {
    public static final String SESSION_ATTRIBUTE = "workOrdersCsrfToken";
    public static final String REQUEST_ATTRIBUTE = "workOrdersCsrfToken";
    public static final String HEADER_NAME = "X-CSRF-Token";
    public static final String PARAMETER_NAME = "_csrf";

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
        throws ServletException, IOException {
        if (isSafeMethod(request.getMethod())) {
            HttpSession session = request.getSession(true);
            String token = (String) session.getAttribute(SESSION_ATTRIBUTE);
            if (token == null) {
                token = UUID.randomUUID().toString();
                session.setAttribute(SESSION_ATTRIBUTE, token);
            }
            request.setAttribute(REQUEST_ATTRIBUTE, token);
        } else {
            HttpSession session = request.getSession(false);
            Object expected = session == null ? null : session.getAttribute(SESSION_ATTRIBUTE);
            String submitted = request.getHeader(HEADER_NAME);
            if (submitted == null) submitted = request.getParameter(PARAMETER_NAME);
            if (!(expected instanceof String token) || submitted == null || !constantTimeEquals(token, submitted)) {
                response.sendError(HttpServletResponse.SC_FORBIDDEN, "CSRF token missing or invalid");
                return;
            }
        }
        chain.doFilter(request, response);
    }

    private static boolean isSafeMethod(String method) {
        return "GET".equals(method) || "HEAD".equals(method) || "OPTIONS".equals(method);
    }

    private static boolean constantTimeEquals(String expected, String submitted) {
        return MessageDigest.isEqual(expected.getBytes(StandardCharsets.UTF_8), submitted.getBytes(StandardCharsets.UTF_8));
    }
}
