from django.contrib import admin
from .models import Employee, WorkOrder, WorkOrderEvent
admin.site.register(Employee)
admin.site.register(WorkOrder)
admin.site.register(WorkOrderEvent)
