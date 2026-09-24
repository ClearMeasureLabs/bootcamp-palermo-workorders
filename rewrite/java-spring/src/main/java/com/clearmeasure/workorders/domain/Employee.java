package com.clearmeasure.workorders.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.JoinTable;
import jakarta.persistence.ManyToMany;
import jakarta.persistence.Table;
import java.util.LinkedHashSet;
import java.util.Set;
import java.util.UUID;

@Entity
@Table(name = "employees")
public class Employee {
    @Id
    @GeneratedValue
    private UUID id;

    @Column(nullable = false, unique = true, length = 100)
    private String username;

    @Column(name = "first_name", nullable = false, length = 100)
    private String firstName;

    @Column(name = "last_name", nullable = false, length = 120)
    private String lastName;

    @Column(name = "email_address", nullable = false, length = 255)
    private String emailAddress;

    @Column(name = "preferred_language", nullable = false, length = 10)
    private String preferredLanguage = "en-US";

    @ManyToMany(fetch = FetchType.EAGER)
    @JoinTable(name = "employee_roles",
        joinColumns = @JoinColumn(name = "employee_id"),
        inverseJoinColumns = @JoinColumn(name = "role_id"))
    private Set<Role> roles = new LinkedHashSet<>();

    protected Employee() { }

    public Employee(String username, String firstName, String lastName, String emailAddress) {
        this.username = username;
        this.firstName = firstName;
        this.lastName = lastName;
        this.emailAddress = emailAddress;
    }

    public UUID getId() { return id; }
    public String getUsername() { return username; }
    public String getFirstName() { return firstName; }
    public String getLastName() { return lastName; }
    public String getEmailAddress() { return emailAddress; }
    public String getPreferredLanguage() { return preferredLanguage; }
    public Set<Role> getRoles() { return Set.copyOf(roles); }
    public String getDisplayName() { return firstName + " " + lastName; }
    public boolean canCreateWorkOrder() { return roles.stream().anyMatch(Role::isCanCreateWorkOrder); }
    public boolean canFulfillWorkOrder() { return roles.stream().anyMatch(Role::isCanFulfillWorkOrder); }
}
