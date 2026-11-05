package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.application.WorkOrderNotFoundException;
import com.clearmeasure.workorders.application.UnauthenticatedException;
import com.clearmeasure.workorders.application.ForbiddenActionException;
import org.springframework.http.HttpStatus;
import org.springframework.orm.ObjectOptimisticLockingFailureException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.*;
import java.util.Map;

@RestControllerAdvice
public class ApiExceptionHandler {
    @ExceptionHandler(UnauthenticatedException.class) @ResponseStatus(HttpStatus.UNAUTHORIZED)
    public Map<String,String> unauthenticated(RuntimeException error) { return Map.of("error", error.getMessage()); }
    @ExceptionHandler({ForbiddenActionException.class, SecurityException.class}) @ResponseStatus(HttpStatus.FORBIDDEN)
    public Map<String,String> forbidden(RuntimeException error) { return Map.of("error", error.getMessage()); }
    @ExceptionHandler(WorkOrderNotFoundException.class) @ResponseStatus(HttpStatus.NOT_FOUND)
    public Map<String,String> notFound(RuntimeException error) { return Map.of("error", error.getMessage()); }
    @ExceptionHandler(ObjectOptimisticLockingFailureException.class) @ResponseStatus(HttpStatus.CONFLICT)
    public Map<String,String> concurrentUpdate(ObjectOptimisticLockingFailureException error) {
        return Map.of("error", "The work order changed in another request. Reload it and try again.");
    }
    @ExceptionHandler({IllegalArgumentException.class, IllegalStateException.class}) @ResponseStatus(HttpStatus.BAD_REQUEST)
    public Map<String,String> invalid(RuntimeException error) { return Map.of("error", error.getMessage()); }
    @ExceptionHandler(MethodArgumentNotValidException.class) @ResponseStatus(HttpStatus.BAD_REQUEST)
    public Map<String,String> validation(MethodArgumentNotValidException error) { return Map.of("error", "Request validation failed"); }
}
