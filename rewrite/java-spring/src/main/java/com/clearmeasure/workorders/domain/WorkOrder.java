package com.clearmeasure.workorders.domain;

import jakarta.persistence.*;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.Clock;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.ZoneId;
import java.util.UUID;

@Entity
@Table(name = "work_orders")
public class WorkOrder {
    @Id
    @GeneratedValue
    private UUID id;

    @Column(nullable = false, unique = true, length = 24)
    private String number;

    @Column(nullable = false, length = 240)
    private String title;

    @Column(length = 4000)
    private String description = "";

    @Column(length = 4000)
    private String instructions = "";

    @Column(name = "room_number", length = 900)
    private String roomNumber;

    @Column(name = "creator_name", length = 160)
    private String creatorName;

    @Column(name = "assignee_name", length = 160)
    private String assigneeName;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 24)
    private WorkOrderStatus status = WorkOrderStatus.DRAFT;

    @Column(name = "created_at", nullable = false)
    private OffsetDateTime createdAt = OffsetDateTime.now();

    @Column(name = "assigned_at")
    private OffsetDateTime assignedAt;

    @Column(name = "completed_at")
    private OffsetDateTime completedAt;

    @Column(name = "due_date")
    private LocalDate dueDate;

    protected WorkOrder() { }

    public WorkOrder(String number, String title, String description, String roomNumber,
                     String creatorName, LocalDate dueDate) {
        this(number, title, description, "", roomNumber, creatorName, dueDate);
    }

    public WorkOrder(String number, String title, String description, String instructions, String roomNumber,
                     String creatorName, LocalDate dueDate) {
        this.number = number;
        this.title = title;
        this.description = description == null ? "" : description;
        this.instructions = instructions == null ? "" : instructions;
        this.roomNumber = roomNumber;
        this.creatorName = creatorName;
        this.dueDate = dueDate;
        this.status = WorkOrderStatus.DRAFT;
    }

    public void assign(String assignee) {
        requireStatus(WorkOrderStatus.DRAFT);
        if (assignee == null || assignee.isBlank()) throw new IllegalArgumentException("Assignee is required");
        this.assigneeName = assignee.trim();
        this.assignedAt = OffsetDateTime.now();
        this.status = WorkOrderStatus.ASSIGNED;
    }

    public void begin() {
        requireStatus(WorkOrderStatus.ASSIGNED);
        this.status = WorkOrderStatus.IN_PROGRESS;
    }

    public void complete() {
        requireStatus(WorkOrderStatus.IN_PROGRESS);
        this.status = WorkOrderStatus.COMPLETE;
        this.completedAt = OffsetDateTime.now();
    }

    public void cancel() {
        if (status == WorkOrderStatus.COMPLETE || status == WorkOrderStatus.CANCELLED)
            throw new IllegalStateException("Closed work orders cannot be cancelled");
        this.status = WorkOrderStatus.CANCELLED;
    }

    private void requireStatus(WorkOrderStatus expected) {
        if (status != expected) throw new IllegalStateException("Expected " + expected + " but was " + status);
    }

    public UUID getId() { return id; }
    public String getNumber() { return number; }
    public String getTitle() { return title; }
    public String getDescription() { return description; }
    public String getInstructions() { return instructions; }
    public String getRoomNumber() { return roomNumber; }
    public String getCreatorName() { return creatorName; }
    public String getAssigneeName() { return assigneeName; }
    public WorkOrderStatus getStatus() { return status; }
    public OffsetDateTime getCreatedAt() { return createdAt; }
    public OffsetDateTime getAssignedAt() { return assignedAt; }
    public OffsetDateTime getCompletedAt() { return completedAt; }
    public LocalDate getDueDate() { return dueDate; }
    @JsonProperty("urgency")
    public DueDateUrgency getUrgency() {
        return DueDateUrgencyCalculator.calculate(dueDate, status, Clock.system(ZoneId.of("America/Chicago")));
    }
    @JsonIgnore
    public String getDueDateCssClass() {
        return switch (getUrgency()) {
            case DUE_TODAY -> "due-date-today";
            case OVERDUE -> "due-date-overdue";
            case NONE -> "";
        };
    }
    @JsonIgnore
    public String getUrgencyBadgeText() {
        if (dueDate == null) return "";
        return switch (getUrgency()) {
            case DUE_TODAY -> "Due Today";
            case OVERDUE -> "Overdue";
            case NONE -> "On Track";
        };
    }
    @JsonIgnore
    public String getUrgencyBadgeCssClass() {
        return switch (getUrgency()) {
            case DUE_TODAY -> "urgency-due-today";
            case OVERDUE -> "urgency-overdue";
            case NONE -> "urgency-on-track";
        };
    }
}
