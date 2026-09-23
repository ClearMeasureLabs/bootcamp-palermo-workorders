from django.db import models
from django.utils import timezone


class Role(models.Model):
    name = models.CharField(max_length=50, unique=True)
    can_create_work_order = models.BooleanField(default=False)
    can_fulfill_work_order = models.BooleanField(default=False)

    def __str__(self):
        return self.name


class Employee(models.Model):
    username = models.CharField(max_length=100, unique=True)
    first_name = models.CharField(max_length=100)
    last_name = models.CharField(max_length=100)
    email = models.EmailField(blank=True)
    active = models.BooleanField(default=True)
    roles = models.ManyToManyField(Role, blank=True, related_name="employees")

    def can_create_work_order(self):
        return self.roles.filter(can_create_work_order=True).exists()

    def can_fulfill_work_order(self):
        return self.roles.filter(can_fulfill_work_order=True).exists()

    def get_full_name(self):
        return f"{self.first_name} {self.last_name}".strip()

    def __str__(self): return f"{self.first_name} {self.last_name}"
class WorkOrder(models.Model):
    class Status(models.TextChoices):
        DRAFT = "Draft", "Draft"
        ASSIGNED = "Assigned", "Assigned"
        IN_PROGRESS = "InProgress", "In Progress"
        COMPLETE = "Complete", "Complete"
        CANCELLED = "Cancelled", "Cancelled"
    number = models.CharField(max_length=30, unique=True, blank=True)
    title = models.CharField(max_length=200)
    description = models.TextField(max_length=4000, blank=True)
    instructions = models.TextField(max_length=4000, blank=True)
    room_number = models.CharField(max_length=900, blank=True)
    status = models.CharField(max_length=20, choices=Status.choices, default=Status.DRAFT)
    creator = models.ForeignKey(Employee, null=True, blank=True, related_name="created_orders", on_delete=models.SET_NULL)
    assignee = models.ForeignKey(Employee, null=True, blank=True, related_name="assigned_orders", on_delete=models.SET_NULL)
    created_at = models.DateTimeField(default=timezone.now)
    assigned_at = models.DateTimeField(null=True, blank=True)
    completed_at = models.DateTimeField(null=True, blank=True)
    due_date = models.DateField(null=True, blank=True)
    def save(self, *args, **kwargs):
        creating = self.pk is None
        super().save(*args, **kwargs)
        if creating and not self.number:
            self.number = f"WO-{self.pk:06d}"
            super().save(update_fields=["number"])
    def __str__(self): return f"{self.number}: {self.title}"
class WorkOrderEvent(models.Model):
    work_order = models.ForeignKey(WorkOrder, related_name="events", on_delete=models.CASCADE)
    from_status = models.CharField(max_length=20, blank=True)
    to_status = models.CharField(max_length=20)
    occurred_at = models.DateTimeField(default=timezone.now)
    note = models.CharField(max_length=500, blank=True)
