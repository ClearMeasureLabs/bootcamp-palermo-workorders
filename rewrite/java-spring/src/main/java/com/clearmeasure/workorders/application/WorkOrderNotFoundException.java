package com.clearmeasure.workorders.application;

public class WorkOrderNotFoundException extends RuntimeException {
    public WorkOrderNotFoundException(String number) { super("Work order not found: " + number); }
}
