from django.db import models
from django.utils import timezone
import uuid


class Role(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False, db_column="Id")
    name = models.CharField(max_length=100, unique=True, db_column="Name")
    can_create_work_order = models.BooleanField(default=False, db_column="CanCreateWorkOrder")
    can_fulfill_work_order = models.BooleanField(default=False, db_column="CanFulfillWorkOrder")

    def __str__(self):
        return self.name

    class Meta:
        db_table = "Role"


class Employee(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False, db_column="Id")
    username = models.CharField(max_length=100, unique=True, db_column="UserName")
    first_name = models.CharField(max_length=100, db_column="FirstName")
    last_name = models.CharField(max_length=120, db_column="LastName")
    email = models.CharField(max_length=255, db_column="EmailAddress")
    preferred_language = models.CharField(max_length=10, default="en-US", db_column="PreferredLanguage")
    roles = models.ManyToManyField(Role, blank=True, related_name="employees", through="EmployeeRole")

    def can_create_work_order(self):
        return self.roles.filter(can_create_work_order=True).exists()

    def can_fulfill_work_order(self):
        return self.roles.filter(can_fulfill_work_order=True).exists()

    def get_full_name(self):
        return f"{self.first_name} {self.last_name}".strip()

    def __str__(self): return f"{self.first_name} {self.last_name}"

    class Meta:
        db_table = "Employee"


class EmployeeRole(models.Model):
    pk = models.CompositePrimaryKey("employee", "role")
    employee = models.ForeignKey(Employee, on_delete=models.CASCADE, db_column="EmployeeId")
    role = models.ForeignKey(Role, on_delete=models.CASCADE, db_column="RoleId")

    class Meta:
        db_table = "EmployeeRoles"
        constraints = [models.UniqueConstraint(fields=["employee", "role"], name="employee_role_unique")]
class WorkOrder(models.Model):
    class Status(models.TextChoices):
        DRAFT = "DRT", "Draft"
        ASSIGNED = "ASD", "Assigned"
        IN_PROGRESS = "IPG", "In Progress"
        COMPLETE = "CMP", "Complete"
        CANCELLED = "CNL", "Cancelled"
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False, db_column="Id")
    number = models.CharField(max_length=7, unique=True, blank=True, db_column="Number")
    title = models.CharField(max_length=300, db_column="Title")
    description = models.TextField(max_length=4000, blank=True, default="", db_column="Description")
    instructions = models.TextField(max_length=4000, blank=True, default="", db_column="Instructions")
    room_number = models.CharField(max_length=900, blank=True, default="", db_column="RoomNumber")
    status = models.CharField(max_length=3, choices=Status.choices, default=Status.DRAFT, db_column="Status")
    creator = models.ForeignKey(Employee, related_name="created_orders", on_delete=models.PROTECT, db_column="CreatorId")
    assignee = models.ForeignKey(Employee, null=True, blank=True, related_name="assigned_orders", on_delete=models.PROTECT, db_column="AssigneeId")
    created_at = models.DateTimeField(default=timezone.now, null=True, blank=True, db_column="CreatedDate")
    assigned_at = models.DateTimeField(null=True, blank=True, db_column="AssignedDate")
    completed_at = models.DateTimeField(null=True, blank=True, db_column="CompletedDate")
    due_date = models.DateField(null=True, blank=True, db_column="DueDate")
    def save(self, *args, **kwargs):
        if not self.number:
            self.number = self.pk.hex[:7].upper()
        super().save(*args, **kwargs)
    def __str__(self): return f"{self.number}: {self.title}"

    class Meta:
        db_table = "WorkOrder"


class WorkOrderEvent(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    work_order = models.ForeignKey(WorkOrder, related_name="events", on_delete=models.CASCADE, db_column="WorkOrderId")
    from_status = models.CharField(max_length=3, blank=True)
    to_status = models.CharField(max_length=3)
    occurred_at = models.DateTimeField(default=timezone.now)
    note = models.CharField(max_length=500, blank=True)


class WorkOrderAttachment(models.Model):
    id = models.UUIDField(primary_key=True, editable=False, db_column="Id")
    work_order = models.ForeignKey(WorkOrder, related_name="attachments", on_delete=models.CASCADE, db_column="WorkOrderId")
    file_name = models.CharField(max_length=500, db_column="FileName")
    content_type = models.CharField(max_length=200, db_column="ContentType")
    file_size = models.BigIntegerField(db_column="FileSize")
    uploaded_by = models.ForeignKey(Employee, related_name="uploaded_attachments", on_delete=models.PROTECT, db_column="UploadedById")
    uploaded_date = models.DateTimeField(db_column="UploadedDate")

    class Meta:
        ordering = ["uploaded_date"]
        db_table = "WorkOrderAttachment"

    def __str__(self):
        return self.file_name
