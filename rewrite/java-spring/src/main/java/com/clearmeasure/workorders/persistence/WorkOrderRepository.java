package com.clearmeasure.workorders.persistence;

import com.clearmeasure.workorders.domain.WorkOrder;
import com.clearmeasure.workorders.domain.WorkOrderStatus;
import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface WorkOrderRepository extends JpaRepository<WorkOrder, UUID> {
    Optional<WorkOrder> findByNumber(String number);
    @Query("select w from WorkOrder w where (:status is null or w.status = :status) " +
           "and (:overdueOnly = false or (w.dueDate < :today and w.status in :openStatuses)) " +
           "order by w.createdAt desc")
    List<WorkOrder> findMatching(@Param("status") WorkOrderStatus status,
                                 @Param("overdueOnly") boolean overdueOnly,
                                 @Param("today") LocalDate today,
                                 @Param("openStatuses") List<WorkOrderStatus> openStatuses);
}
