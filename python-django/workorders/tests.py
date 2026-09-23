from django.test import TestCase
from django.urls import reverse
from django.core.exceptions import ValidationError
from .models import Employee, WorkOrder, WorkOrderEvent
from .services import transition

class WorkOrderDomainTests(TestCase):
    def setUp(self):
        self.employee = Employee.objects.create(username="maint", first_name="Morgan", last_name="Lee")
        self.order = WorkOrder.objects.create(title="Repair sink", creator=self.employee, room_number="204")
    def test_number_is_assigned_and_status_defaults_to_draft(self):
        self.assertRegex(self.order.number, r"^WO-\d{6}$")
        self.assertEqual(self.order.status, WorkOrder.Status.DRAFT)
    def test_valid_lifecycle_records_events_and_timestamps(self):
        transition(self.order, WorkOrder.Status.ASSIGNED, "Accepted")
        transition(self.order, WorkOrder.Status.IN_PROGRESS)
        transition(self.order, WorkOrder.Status.COMPLETE)
        self.order.refresh_from_db()
        self.assertIsNotNone(self.order.assigned_at)
        self.assertIsNotNone(self.order.completed_at)
        self.assertEqual(self.order.events.count(), 3)
        self.assertEqual(self.order.events.first().note, "Accepted")
    def test_invalid_lifecycle_transition_is_rejected(self):
        with self.assertRaises(ValidationError):
            transition(self.order, WorkOrder.Status.COMPLETE)
        self.order.refresh_from_db()
        self.assertEqual(self.order.status, WorkOrder.Status.DRAFT)
        self.assertEqual(WorkOrderEvent.objects.count(), 0)

class WorkOrderWebTests(TestCase):
    def test_health_is_json(self):
        response = self.client.get(reverse("health"))
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "Healthy")
    def test_create_search_filter_detail_and_status_transition(self):
        employee = Employee.objects.create(username="jlee", first_name="Jordan", last_name="Lee")
        response = self.client.post(reverse("work_order_create"), {"title": "Replace light", "description": "Hallway", "instructions": "Use ladder", "room_number": "101", "creator": employee.pk, "assignee": employee.pk, "due_date": ""})
        order = WorkOrder.objects.get(title="Replace light")
        self.assertEqual(response.status_code, 302)
        self.assertIn(order.number, self.client.get(reverse("work_order_list") + "?q=WO-").content.decode())
        self.assertEqual(self.client.get(reverse("work_order_detail", args=[order.pk])).status_code, 200)
        self.client.post(reverse("work_order_transition", args=[order.pk]), {"status": WorkOrder.Status.ASSIGNED})
        order.refresh_from_db()
        self.assertEqual(order.status, WorkOrder.Status.ASSIGNED)
        self.assertEqual(order.events.count(), 1)
    def test_list_search_matches_room_and_title(self):
        WorkOrder.objects.create(title="Inspect boiler", room_number="B-2")
        response = self.client.get(reverse("work_order_list"), {"q": "B-2"})
        self.assertContains(response, "Inspect boiler")
        self.assertNotContains(response, "Repair sink")
