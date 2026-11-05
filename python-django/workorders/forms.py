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
        self.fields["assignee"].queryset = Employee.objects.order_by("last_name", "first_name")


class AttachmentMetadataForm(forms.Form):
    file_name = forms.CharField(max_length=500, label="File name")
    content_type = forms.CharField(max_length=200, label="Content type")
    file_size = forms.IntegerField(min_value=0, max_value=2**63 - 1, label="File size (bytes)")
