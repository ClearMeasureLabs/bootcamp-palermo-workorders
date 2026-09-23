package com.clearmeasure.workorders.domain;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

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
}
