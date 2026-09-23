from .models import Employee


def employee_context(request):
    employee_id = request.session.get("workorders_employee_id")
    employee = Employee.objects.filter(pk=employee_id, active=True).first() if employee_id else None
    return {"current_employee": employee}
