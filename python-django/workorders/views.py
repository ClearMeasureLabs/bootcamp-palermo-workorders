from django.contrib import messages
from django.conf import settings
from django.core.exceptions import ValidationError
from django.db import connection
from django.db.models import Count, Q
from django.http import HttpResponse, HttpResponseForbidden, JsonResponse
from django.shortcuts import get_object_or_404, redirect, render
from django.views.decorators.http import require_POST
from uuid import UUID
from .forms import AttachmentMetadataForm, WorkOrderForm
from .models import Employee, WorkOrder
from .services import OPEN_STATUSES, add_attachment_metadata, available_transitions, chicago_today, due_date_badge, due_date_urgency, transition
from .operations import operational_api

SESSION_EMPLOYEE_KEY = "workorders_employee_id"


def current_employee(request):
    employee_id = request.session.get(SESSION_EMPLOYEE_KEY)
    if employee_id is None:
        return None
    return Employee.objects.filter(pk=employee_id).first()


def login(request):
    if not settings.ENABLE_DEMO_LOGIN:
        return HttpResponse("Passwordless demo login is disabled.", status=503)
    employees = Employee.objects.order_by("last_name", "first_name")
    error = ""
    if request.method == "POST":
        username = request.POST.get("username", "")
        employee = employees.filter(username=username).first()
        if employee:
            request.session.cycle_key()
            request.session[SESSION_EMPLOYEE_KEY] = str(employee.pk)
            return redirect("work_order_list")
        error = "Select a valid employee."
    return render(request, "workorders/login.html", {"employees": employees, "error": error})


@require_POST
def logout(request):
    request.session.pop(SESSION_EMPLOYEE_KEY, None)
    return redirect("login")
def work_order_list(request):
    actor = current_employee(request)
    if actor is None:
        return redirect("login")
    orders = WorkOrder.objects.select_related("assignee", "creator").order_by("-created_at")
    query = request.GET.get("q", "").strip()
    status = request.GET.get("status", "")
    overdue_only = request.GET.get("overdue") == "1"
    creator_id = request.GET.get("creator", "")
    assignee_id = request.GET.get("assignee", "")
    assigned_to_me = request.GET.get("assigned_to_me") == "1"
    sort = request.GET.get("sort", "")
    today = chicago_today()
    if query: orders = orders.filter(Q(number__icontains=query) | Q(title__icontains=query) | Q(room_number__icontains=query))
    if status in WorkOrder.Status.values: orders = orders.filter(status=status)
    try: orders = orders.filter(creator_id=UUID(creator_id)) if creator_id else orders
    except ValueError: pass
    if assigned_to_me and actor:
        orders = orders.filter(assignee=actor)
    elif assignee_id:
        try: orders = orders.filter(assignee_id=UUID(assignee_id))
        except ValueError: pass
    if overdue_only: orders = orders.filter(due_date__lt=today, status__in=OPEN_STATUSES)
    sort_fields = {"Status": "status", "Title": "title", "DueDate": "due_date", "Room": "room_number"}
    if sort in sort_fields:
        field = sort_fields[sort]
        direction = request.GET.get("direction", "asc")
        orders = orders.order_by(("-" if direction == "desc" else "") + field, "number")
    else:
        direction = "asc"
        orders = orders.order_by("-created_at")
    orders = list(orders)
    for order in orders:
        order.due_urgency = due_date_urgency(order, today)
        order.due_badge = due_date_badge(order, today)
        order.due_css_class = {"DueToday": "due-date-today", "Overdue": "due-date-overdue"}.get(order.due_urgency, "")
    counts = {status: 0 for status, _ in WorkOrder.Status.choices}
    counts.update(WorkOrder.objects.values("status").annotate(total=Count("id")).values_list("status", "total"))
    sort_names = ("Status", "Title", "DueDate", "Room")
    next_directions = {name: "desc" if sort == name and direction == "asc" else "asc" for name in sort_names}
    return render(request, "workorders/list.html", {"orders": orders, "query": query, "selected_status": status, "statuses": WorkOrder.Status.choices, "counts": counts, "overdue_only": overdue_only, "current_employee": actor, "employees": Employee.objects.order_by("last_name", "first_name"), "creator_id": creator_id, "assignee_id": assignee_id, "assigned_to_me": assigned_to_me, "sort": sort, "direction": direction, "next_directions": next_directions})
def work_order_create(request):
    actor = current_employee(request)
    if actor is None:
        return redirect("login")
    if not actor.can_create_work_order():
        return HttpResponseForbidden("Your role cannot create work orders.")
    form = WorkOrderForm(request.POST or None)
    if form.is_valid():
        order = form.save(commit=False)
        order.creator = actor
        order.save()
        messages.success(request, f"Work order {order.number} created.")
        return redirect("work_order_detail", pk=order.pk)
    return render(request, "workorders/form.html", {"form": form})
def work_order_detail(request, pk):
    actor = current_employee(request)
    if actor is None:
        return redirect("login")
    order = get_object_or_404(WorkOrder.objects.select_related("creator", "assignee").prefetch_related("events"), pk=pk)
    attachments = order.attachments.select_related("uploaded_by").order_by("uploaded_date")
    return render(request, "workorders/detail.html", {"order": order, "available_transitions": available_transitions(order, actor), "attachments": attachments, "attachment_form": AttachmentMetadataForm() if actor else None})


@require_POST
def work_order_attachment_create(request, pk):
    actor = current_employee(request)
    if actor is None:
        return redirect("login")
    order = get_object_or_404(WorkOrder, pk=pk)
    form = AttachmentMetadataForm(request.POST)
    if form.is_valid():
        attachment = add_attachment_metadata(order, actor, **form.cleaned_data)
        messages.success(request, f"Attachment metadata for {attachment.file_name} added.")
        return redirect("work_order_detail", pk=order.pk)
    order = get_object_or_404(WorkOrder.objects.select_related("creator", "assignee").prefetch_related("events"), pk=pk)
    attachments = order.attachments.select_related("uploaded_by").order_by("uploaded_date")
    return render(request, "workorders/detail.html", {"order": order, "available_transitions": available_transitions(order, actor), "attachments": attachments, "attachment_form": form})
@require_POST
def work_order_transition(request, pk):
    actor = current_employee(request)
    if actor is None:
        return redirect("login")
    order = get_object_or_404(WorkOrder, pk=pk)
    try:
        order = transition(order, request.POST.get("status", ""), actor, request.POST.get("note", ""))
        messages.success(request, f"Work order {order.number} moved to {order.get_status_display()}.")
    except ValidationError as error:
        messages.error(request, " ".join(error.messages))
    return redirect("work_order_detail", pk=order.pk)
def health(request):
    try:
        connection.ensure_connection()
    except Exception:
        return JsonResponse({"status": "Unhealthy", "service": "workorders", "database": "unavailable"}, status=503)
    return JsonResponse({"status": "Healthy", "service": "workorders", "database": "connected"})


def api_operations(request, path=""):
    return operational_api(request, path)
