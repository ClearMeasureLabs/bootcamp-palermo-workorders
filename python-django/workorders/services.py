from datetime import date
from zoneinfo import ZoneInfo

from django.core.exceptions import ValidationError
from django.utils import timezone
from .models import WorkOrder, WorkOrderEvent
TRANSITIONS = {WorkOrder.Status.DRAFT: {WorkOrder.Status.ASSIGNED, WorkOrder.Status.CANCELLED}, WorkOrder.Status.ASSIGNED: {WorkOrder.Status.IN_PROGRESS, WorkOrder.Status.CANCELLED}, WorkOrder.Status.IN_PROGRESS: {WorkOrder.Status.ASSIGNED, WorkOrder.Status.COMPLETE}, WorkOrder.Status.COMPLETE: set(), WorkOrder.Status.CANCELLED: set()}

CHICAGO = ZoneInfo("America/Chicago")
OPEN_STATUSES = (WorkOrder.Status.DRAFT, WorkOrder.Status.ASSIGNED, WorkOrder.Status.IN_PROGRESS)


def chicago_today() -> date:
    return timezone.localdate(timezone.now(), CHICAGO)


def due_date_urgency(work_order: WorkOrder, today: date | None = None) -> str | None:
    """Return read-time urgency using the Chicago calendar date."""
    if work_order.due_date is None or work_order.status not in OPEN_STATUSES:
        return None
    today = today or chicago_today()
    if work_order.due_date < today:
        return "Overdue"
    if work_order.due_date == today:
        return "DueToday"
    return None


def due_date_badge(work_order: WorkOrder, today: date | None = None) -> str | None:
    if work_order.due_date is None:
        return None
    urgency = due_date_urgency(work_order, today)
    return {"Overdue": "Overdue", "DueToday": "Due Today"}.get(urgency, "On Track")


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
