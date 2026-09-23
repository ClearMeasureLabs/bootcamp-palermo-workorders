from django.contrib import messages
from django.core.exceptions import ValidationError
from django.db import connection
from django.db.models import Count, Q
from django.http import HttpResponseForbidden, JsonResponse
from django.shortcuts import get_object_or_404, redirect, render
from django.views.decorators.http import require_POST
from .forms import WorkOrderForm
from .models import Employee, WorkOrder
from .services import OPEN_STATUSES, available_transitions, chicago_today, due_date_badge, due_date_urgency, transition

SESSION_EMPLOYEE_KEY = "workorders_employee_id"


def current_employee(request):
    employee_id = request.session.get(SESSION_EMPLOYEE_KEY)
    if employee_id is None:
        return None
    return Employee.objects.filter(pk=employee_id, active=True).first()


def login(request):
    employees = Employee.objects.filter(active=True).order_by("last_name", "first_name")
    error = ""
    if request.method == "POST":
        username = request.POST.get("username", "")
        employee = employees.filter(username=username).first()
        if employee:
            request.session.cycle_key()
            request.session[SESSION_EMPLOYEE_KEY] = employee.pk
            return redirect("work_order_list")
        error = "Select a valid employee."
    return render(request, "workorders/login.html", {"employees": employees, "error": error})


@require_POST
def logout(request):
    request.session.pop(SESSION_EMPLOYEE_KEY, None)
    return redirect("login")
def work_order_list(request):
    actor = current_employee(request)
    orders = WorkOrder.objects.select_related("assignee", "creator").order_by("-created_at")
    query = request.GET.get("q", "").strip()
    status = request.GET.get("status", "")
    overdue_only = request.GET.get("overdue") == "1"
    today = chicago_today()
    if query: orders = orders.filter(Q(number__icontains=query) | Q(title__icontains=query) | Q(room_number__icontains=query))
    if status in WorkOrder.Status.values: orders = orders.filter(status=status)
    if overdue_only: orders = orders.filter(due_date__lt=today, status__in=OPEN_STATUSES)
    orders = list(orders)
    for order in orders:
        order.due_urgency = due_date_urgency(order, today)
        order.due_badge = due_date_badge(order, today)
        order.due_css_class = {"DueToday": "due-date-today", "Overdue": "due-date-overdue"}.get(order.due_urgency, "")
    counts = {status: 0 for status, _ in WorkOrder.Status.choices}
    counts.update(WorkOrder.objects.values("status").annotate(total=Count("id")).values_list("status", "total"))
    return render(request, "workorders/list.html", {"orders": orders, "query": query, "selected_status": status, "statuses": WorkOrder.Status.choices, "counts": counts, "overdue_only": overdue_only, "current_employee": actor})
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
    order = get_object_or_404(WorkOrder.objects.select_related("creator", "assignee").prefetch_related("events"), pk=pk)
    return render(request, "workorders/detail.html", {"order": order, "available_transitions": available_transitions(order, actor)})
@require_POST
def work_order_transition(request, pk):
    order = get_object_or_404(WorkOrder, pk=pk)
    try:
        transition(order, request.POST.get("status", ""), current_employee(request), request.POST.get("note", ""))
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
