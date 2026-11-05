package com.clearmeasure.workorders.persistence;

import com.clearmeasure.workorders.domain.Employee;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface EmployeeRepository extends JpaRepository<Employee, UUID> {
    Optional<Employee> findByUsername(String username);
    List<Employee> findAllByOrderByLastNameAscFirstNameAsc();
}
