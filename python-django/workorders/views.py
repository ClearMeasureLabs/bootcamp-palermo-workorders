from django.contrib import messages
from django.core.exceptions import ValidationError
from django.db.models import Q
from django.http import JsonResponse
from django.shortcuts import get_object_or_404, redirect, render
from django.views.decorators.http import require_POST
from .forms import WorkOrderForm
from .models import WorkOrder
from .services import transition
def work_order_list(request):
    orders = WorkOrder.objects.select_related("assignee", "creator").order_by("-created_at")
    query = request.GET.get("q", "").strip()
    status = request.GET.get("status", "")
    if query: orders = orders.filter(Q(number__icontains=query) | Q(title__icontains=query) | Q(room_number__icontains=query))
    if status in WorkOrder.Status.values: orders = orders.filter(status=status)
    counts = {s: WorkOrder.objects.filter(status=s).count() for s, _ in WorkOrder.Status.choices}
    return render(request, "workorders/list.html", {"orders": orders, "query": query, "selected_status": status, "statuses": WorkOrder.Status.choices, "counts": counts})
def work_order_create(request):
    form = WorkOrderForm(request.POST or None)
    if form.is_valid():
        order = form.save()
        messages.success(request, f"Work order {order.number} created.")
        return redirect("work_order_detail", pk=order.pk)
    return render(request, "workorders/form.html", {"form": form})
def work_order_detail(request, pk):
    order = get_object_or_404(WorkOrder.objects.select_related("creator", "assignee").prefetch_related("events"), pk=pk)
    return render(request, "workorders/detail.html", {"order": order})
@require_POST
def work_order_transition(request, pk):
    order = get_object_or_404(WorkOrder, pk=pk)
    try:
        transition(order, request.POST.get("status", ""), request.POST.get("note", ""))
        messages.success(request, f"Work order {order.number} moved to {order.get_status_display()}.")
    except ValidationError as error:
        messages.error(request, " ".join(error.messages))
    return redirect("work_order_detail", pk=order.pk)
def health(request): return JsonResponse({"status": "Healthy", "service": "workorders", "database": "configured"})
