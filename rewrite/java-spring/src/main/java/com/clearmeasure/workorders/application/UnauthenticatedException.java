package com.clearmeasure.workorders.application;

public class UnauthenticatedException extends RuntimeException {
    public UnauthenticatedException() { super("Select an employee before continuing"); }
}
