package com.clearmeasure.workorders.persistence;

import com.clearmeasure.workorders.domain.WorkOrderStatus;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class WorkOrderStatusConverterTest {
    private final WorkOrderStatusConverter converter = new WorkOrderStatusConverter();

    @Test void storesSourceStatusCodesAndReadsThemBack() {
        assertEquals("DRT", converter.convertToDatabaseColumn(WorkOrderStatus.DRAFT));
        assertEquals("ASD", converter.convertToDatabaseColumn(WorkOrderStatus.ASSIGNED));
        assertEquals("IPG", converter.convertToDatabaseColumn(WorkOrderStatus.IN_PROGRESS));
        assertEquals("CMP", converter.convertToDatabaseColumn(WorkOrderStatus.COMPLETE));
        assertEquals("CNL", converter.convertToDatabaseColumn(WorkOrderStatus.CANCELLED));
        for (WorkOrderStatus status : WorkOrderStatus.values())
            assertEquals(status, converter.convertToEntityAttribute(converter.convertToDatabaseColumn(status)));
    }

    @Test void rejectsUnknownCodes() {
        assertThrows(IllegalArgumentException.class, () -> converter.convertToEntityAttribute("BAD"));
    }
}
