package com.clearmeasure.workorders.application;

public class ForbiddenActionException extends RuntimeException {
    public ForbiddenActionException(String message) { super(message); }
}
