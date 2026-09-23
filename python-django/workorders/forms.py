from django import forms
from .models import Employee, WorkOrder
class WorkOrderForm(forms.ModelForm):
    class Meta:
        model = WorkOrder
        fields = ["title", "description", "instructions", "room_number", "assignee", "due_date"]
        widgets = {"due_date": forms.DateInput(attrs={"type": "date"})}
        labels = {"room_number": "Room number", "due_date": "Due date"}
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.fields["assignee"].queryset = Employee.objects.filter(active=True).order_by("last_name", "first_name")
