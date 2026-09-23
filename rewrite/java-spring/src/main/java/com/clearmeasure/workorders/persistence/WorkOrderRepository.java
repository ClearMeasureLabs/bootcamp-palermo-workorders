package com.clearmeasure.workorders.persistence;

import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface WorkOrderRepository extends JpaRepository<WorkOrder, UUID> {
    Optional<WorkOrder> findByNumber(String number);
    List<WorkOrder> findAllByOrderByCreatedAtDesc();
    List<WorkOrder> findByStatusOrderByCreatedAtDesc(WorkOrderStatus status);
}
