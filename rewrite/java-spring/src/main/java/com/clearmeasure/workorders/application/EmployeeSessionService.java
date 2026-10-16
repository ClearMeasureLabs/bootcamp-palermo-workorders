package com.clearmeasure.workorders.application;

import com.clearmeasure.workorders.domain.Employee;
import com.clearmeasure.workorders.domain.Role;
import com.clearmeasure.workorders.persistence.EmployeeRepository;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpSession;
import java.util.List;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class EmployeeSessionService {
    private static final String EMPLOYEE_USERNAME = "authenticatedEmployeeUsername";
    private final EmployeeRepository employees;

    public EmployeeSessionService(EmployeeRepository employees) { this.employees = employees; }

    @Transactional(readOnly = true)
    public EmployeeView login(String username, HttpServletRequest request) {
        if (username == null || username.isBlank()) throw new IllegalArgumentException("Employee selection is required");
        Employee employee = employees.findByUsername(username).orElseThrow(
            () -> new IllegalArgumentException("Invalid employee selection"));
        HttpSession session = request.getSession(false);
        if (session == null) {
            session = request.getSession(true);
        } else {
            request.changeSessionId();
        }
        session.setAttribute(EMPLOYEE_USERNAME, employee.getUsername());
        return view(employee);
    }

    @Transactional(readOnly = true)
    public Employee requireCurrent(HttpSession session) {
        if (session == null) throw new UnauthenticatedException();
        Object username = session.getAttribute(EMPLOYEE_USERNAME);
        if (!(username instanceof String value)) throw new UnauthenticatedException();
        return employees.findByUsername(value).orElseThrow(UnauthenticatedException::new);
    }

    @Transactional(readOnly = true)
    public Employee currentOrNull(HttpSession session) {
        if (session == null) return null;
        Object username = session.getAttribute(EMPLOYEE_USERNAME);
        if (!(username instanceof String value)) return null;
        return employees.findByUsername(value).orElse(null);
    }

    @Transactional(readOnly = true)
    public EmployeeView currentView(HttpSession session) { return view(requireCurrent(session)); }

    public void logout(HttpSession session) {
        if (session != null) session.invalidate();
    }

    @Transactional(readOnly = true)
    public List<Employee> employeeChoices() {
        return employees.findAllByOrderByLastNameAscFirstNameAsc();
    }

    @Transactional(readOnly = true)
    public List<Employee> fulfillmentChoices() {
        return employeeChoices().stream().filter(Employee::canFulfillWorkOrder).toList();
    }

    private static EmployeeView view(Employee employee) {
        return new EmployeeView(employee.getUsername(), employee.getDisplayName(), employee.canCreateWorkOrder(),
            employee.canFulfillWorkOrder(), employee.getRoles().stream().map(Role::getName).sorted().toList());
    }

    public record EmployeeView(String username, String displayName, boolean canCreateWorkOrder,
                               boolean canFulfillWorkOrder, List<String> roles) { }
}
