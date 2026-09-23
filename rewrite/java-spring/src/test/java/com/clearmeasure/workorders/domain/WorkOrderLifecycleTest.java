package com.clearmeasure.workorders.domain;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.time.Clock;
import java.time.Instant;
import java.time.ZoneId;
import java.time.LocalDate;

class WorkOrderLifecycleTest {
    @Test void followsDraftAssignedInProgressCompleteLifecycle() {
        WorkOrder order = new WorkOrder("WO-1", "Fix leak", "", "204", "Mara", null);
        assertEquals(WorkOrderStatus.DRAFT, order.getStatus());
        order.assign("Lee"); assertEquals(WorkOrderStatus.ASSIGNED, order.getStatus());
        assertNotNull(order.getAssignedAt());
        order.begin(); assertEquals(WorkOrderStatus.IN_PROGRESS, order.getStatus());
        order.complete(); assertEquals(WorkOrderStatus.COMPLETE, order.getStatus());
        assertNotNull(order.getCompletedAt());
    }

    @Test void rejectsInvalidTransitionAndEmptyAssignee() {
        WorkOrder order = new WorkOrder("WO-2", "Repair", "", "103", "Mara", null);
        assertThrows(IllegalStateException.class, order::begin);
        assertThrows(IllegalArgumentException.class, () -> order.assign("  "));
        order.assign("Lee");
        assertThrows(IllegalStateException.class, order::complete);
    }

    @Test void computesUrgencyUsingChicagoDateOnlyForOpenOrders() {
        Clock beforeChicagoMidnight = Clock.fixed(Instant.parse("2026-09-23T04:30:00Z"), ZoneId.of("America/Chicago"));
        assertEquals(LocalDate.of(2026, 9, 22), LocalDate.now(beforeChicagoMidnight));
        assertEquals(DueDateUrgency.DUE_TODAY, DueDateUrgencyCalculator.calculate(
            LocalDate.of(2026, 9, 22), WorkOrderStatus.IN_PROGRESS, beforeChicagoMidnight));
        assertEquals(DueDateUrgency.OVERDUE, DueDateUrgencyCalculator.calculate(
            LocalDate.of(2026, 9, 21), WorkOrderStatus.DRAFT, beforeChicagoMidnight));
        assertEquals(DueDateUrgency.NONE, DueDateUrgencyCalculator.calculate(
            LocalDate.of(2026, 9, 22), WorkOrderStatus.COMPLETE, beforeChicagoMidnight));
        assertEquals(DueDateUrgency.NONE, DueDateUrgencyCalculator.calculate(
            null, WorkOrderStatus.DRAFT, beforeChicagoMidnight));
    }
}
