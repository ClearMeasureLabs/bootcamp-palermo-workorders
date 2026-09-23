from django.core.exceptions import ValidationError
from django.utils import timezone
from .models import WorkOrder, WorkOrderEvent
TRANSITIONS = {WorkOrder.Status.DRAFT: {WorkOrder.Status.ASSIGNED, WorkOrder.Status.CANCELLED}, WorkOrder.Status.ASSIGNED: {WorkOrder.Status.IN_PROGRESS, WorkOrder.Status.CANCELLED}, WorkOrder.Status.IN_PROGRESS: {WorkOrder.Status.ASSIGNED, WorkOrder.Status.COMPLETE}, WorkOrder.Status.COMPLETE: set(), WorkOrder.Status.CANCELLED: set()}
def transition(work_order: WorkOrder, target: str, note: str = "") -> WorkOrder:
    if target not in TRANSITIONS.get(work_order.status, set()):
        raise ValidationError(f"Cannot change status from {work_order.get_status_display()} to {target}.")
    previous = work_order.status
    work_order.status = target
    if target == WorkOrder.Status.ASSIGNED: work_order.assigned_at = timezone.now()
    if target == WorkOrder.Status.COMPLETE: work_order.completed_at = timezone.now()
    work_order.save()
    WorkOrderEvent.objects.create(work_order=work_order, from_status=previous, to_status=target, note=note)
    return work_order
