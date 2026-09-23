from datetime import date
from zoneinfo import ZoneInfo

from django.core.exceptions import ValidationError
from django.db import transaction
from django.utils import timezone
from .models import Employee, WorkOrder, WorkOrderEvent
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


ACTOR_FOR_TRANSITION = {
    (WorkOrder.Status.DRAFT, WorkOrder.Status.ASSIGNED): "creator",
    (WorkOrder.Status.DRAFT, WorkOrder.Status.CANCELLED): "creator",
    (WorkOrder.Status.ASSIGNED, WorkOrder.Status.IN_PROGRESS): "assignee",
    (WorkOrder.Status.ASSIGNED, WorkOrder.Status.CANCELLED): "creator",
    (WorkOrder.Status.IN_PROGRESS, WorkOrder.Status.ASSIGNED): "assignee",
    (WorkOrder.Status.IN_PROGRESS, WorkOrder.Status.COMPLETE): "assignee",
}


def available_transitions(work_order: WorkOrder, actor: Employee | None) -> list[str]:
    if actor is None:
        return []
    allowed = []
    for target in TRANSITIONS.get(work_order.status, set()):
        actor_field = ACTOR_FOR_TRANSITION.get((work_order.status, target))
        if actor_field and getattr(work_order, f"{actor_field}_id") == actor.pk:
            allowed.append(target)
    return [status for status, _ in WorkOrder.Status.choices if status in allowed]


def transition(work_order: WorkOrder, target: str, actor: Employee | None, note: str = "") -> WorkOrder:
    if len(note) > WorkOrderEvent._meta.get_field("note").max_length:
        raise ValidationError("Transition note must be 500 characters or fewer.")
    with transaction.atomic():
        current = WorkOrder.objects.select_for_update().get(pk=work_order.pk)
        if target not in TRANSITIONS.get(current.status, set()):
            target_label = dict(WorkOrder.Status.choices).get(target, target)
            raise ValidationError(f"Cannot change status from {current.get_status_display()} to {target_label}.")
        actor_field = ACTOR_FOR_TRANSITION.get((current.status, target))
        if actor is None or actor_field is None or actor.pk != getattr(current, f"{actor_field}_id"):
            raise ValidationError("You are not allowed to perform this work order action.")
        previous = current.status
        current.status = target
        if target == WorkOrder.Status.ASSIGNED:
            current.assigned_at = timezone.now()
        if target == WorkOrder.Status.COMPLETE:
            current.completed_at = timezone.now()
        if target == WorkOrder.Status.CANCELLED and previous == WorkOrder.Status.ASSIGNED:
            current.assigned_at = None
            current.assignee = None
        current.save()
        WorkOrderEvent.objects.create(work_order=current, from_status=previous, to_status=target, note=note)
        return current
