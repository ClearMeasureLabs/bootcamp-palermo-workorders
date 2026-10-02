package com.clearmeasure.workorders.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.util.UUID;

@Entity
@Table(name = "roles")
public class Role {
    @Id
    @GeneratedValue
    private UUID id;

    @Column(nullable = false, unique = true, length = 100)
    private String name;

    @Column(name = "can_create_work_order", nullable = false)
    private boolean canCreateWorkOrder;

    @Column(name = "can_fulfill_work_order", nullable = false)
    private boolean canFulfillWorkOrder;

    protected Role() { }

    public Role(String name, boolean canCreateWorkOrder, boolean canFulfillWorkOrder) {
        this.name = name;
        this.canCreateWorkOrder = canCreateWorkOrder;
        this.canFulfillWorkOrder = canFulfillWorkOrder;
    }

    public UUID getId() { return id; }
    public String getName() { return name; }
    public boolean isCanCreateWorkOrder() { return canCreateWorkOrder; }
    public boolean isCanFulfillWorkOrder() { return canFulfillWorkOrder; }
}
