from django.contrib import admin
from .models import Employee, WorkOrder, WorkOrderEvent
admin.site.register(Employee)


@admin.register(WorkOrder)
class WorkOrderAdmin(admin.ModelAdmin):
    readonly_fields = ("number", "status", "creator", "assignee", "created_at", "assigned_at", "completed_at")


@admin.register(WorkOrderEvent)
class WorkOrderEventAdmin(admin.ModelAdmin):
    readonly_fields = ("work_order", "from_status", "to_status", "occurred_at", "note")

    def has_add_permission(self, request):
        return False

    def has_delete_permission(self, request, obj=None):
        return False
