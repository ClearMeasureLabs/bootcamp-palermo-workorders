package com.clearmeasure.workorders.persistence;

import com.clearmeasure.workorders.domain.WorkOrderStatus;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

@Converter
public class WorkOrderStatusConverter implements AttributeConverter<WorkOrderStatus, String> {
    @Override
    public String convertToDatabaseColumn(WorkOrderStatus status) {
        if (status == null) return null;
        return switch (status) {
            case DRAFT -> "DRT";
            case ASSIGNED -> "ASD";
            case IN_PROGRESS -> "IPG";
            case COMPLETE -> "CMP";
            case CANCELLED -> "CNL";
        };
    }

    @Override
    public WorkOrderStatus convertToEntityAttribute(String code) {
        if (code == null) return null;
        return switch (code.trim()) {
            case "DRT" -> WorkOrderStatus.DRAFT;
            case "ASD" -> WorkOrderStatus.ASSIGNED;
            case "IPG" -> WorkOrderStatus.IN_PROGRESS;
            case "CMP" -> WorkOrderStatus.COMPLETE;
            case "CNL" -> WorkOrderStatus.CANCELLED;
            default -> throw new IllegalArgumentException("Unknown work-order status code: " + code);
        };
    }
}
