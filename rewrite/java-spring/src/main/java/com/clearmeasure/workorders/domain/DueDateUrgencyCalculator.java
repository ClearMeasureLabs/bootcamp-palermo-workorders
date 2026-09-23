package com.clearmeasure.workorders.domain;

import java.time.Clock;
import java.time.LocalDate;

/** Computes due date urgency at read time using the configured clock's calendar date. */
public final class DueDateUrgencyCalculator {
    private DueDateUrgencyCalculator() { }

    public static DueDateUrgency calculate(LocalDate dueDate, WorkOrderStatus status, Clock clock) {
        if (dueDate == null || status == WorkOrderStatus.COMPLETE || status == WorkOrderStatus.CANCELLED) {
            return DueDateUrgency.NONE;
        }
        LocalDate today = LocalDate.now(clock);
        if (dueDate.isBefore(today)) return DueDateUrgency.OVERDUE;
        if (dueDate.isEqual(today)) return DueDateUrgency.DUE_TODAY;
        return DueDateUrgency.NONE;
    }
}
