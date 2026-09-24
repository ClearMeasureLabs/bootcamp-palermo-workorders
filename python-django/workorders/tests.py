from django.test import TestCase, override_settings
from django.urls import reverse
from django.core.exceptions import ValidationError
from datetime import date, datetime, timedelta, timezone as datetime_timezone
from unittest.mock import patch
from django.contrib import admin
from .models import Employee, Role, WorkOrder, WorkOrderAttachment, WorkOrderEvent
from .services import available_transitions, chicago_today, due_date_badge, due_date_urgency, transition

class WorkOrderDomainTests(TestCase):
    def setUp(self):
        self.employee = Employee.objects.create(username="maint", first_name="Morgan", last_name="Lee")
        self.order = WorkOrder.objects.create(title="Repair sink", creator=self.employee, assignee=self.employee, room_number="204")
    def test_number_is_assigned_and_status_defaults_to_draft(self):
        self.assertRegex(self.order.number, r"^WO-\d{6}$")
        self.assertEqual(self.order.status, WorkOrder.Status.DRAFT)
    def test_valid_lifecycle_records_events_and_timestamps(self):
        transition(self.order, WorkOrder.Status.ASSIGNED, self.employee, "Accepted")
        transition(self.order, WorkOrder.Status.IN_PROGRESS, self.employee)
        transition(self.order, WorkOrder.Status.COMPLETE, self.employee)
        self.order.refresh_from_db()
        self.assertIsNotNone(self.order.assigned_at)
        self.assertIsNotNone(self.order.completed_at)
        self.assertEqual(self.order.events.count(), 3)
        self.assertEqual(self.order.events.first().note, "Accepted")
    def test_invalid_lifecycle_transition_is_rejected(self):
        with self.assertRaises(ValidationError):
            transition(self.order, WorkOrder.Status.COMPLETE, self.employee)
        self.order.refresh_from_db()
        self.assertEqual(self.order.status, WorkOrder.Status.DRAFT)
        self.assertEqual(WorkOrderEvent.objects.count(), 0)

    def test_transition_reloads_state_and_rolls_back_when_history_write_fails(self):
        stale_order = WorkOrder.objects.get(pk=self.order.pk)
        transition(self.order, WorkOrder.Status.ASSIGNED, self.employee)
        with self.assertRaises(ValidationError):
            transition(stale_order, WorkOrder.Status.ASSIGNED, self.employee)
        order = WorkOrder.objects.get(pk=self.order.pk)
        with patch("workorders.services.WorkOrderEvent.objects.create", side_effect=RuntimeError("event insert failed")):
            with self.assertRaises(RuntimeError):
                transition(order, WorkOrder.Status.IN_PROGRESS, self.employee)
        order.refresh_from_db()
        self.assertEqual(order.status, WorkOrder.Status.ASSIGNED)
        self.assertEqual(order.events.count(), 1)

    def test_transition_rejects_oversized_note_without_persisting_change(self):
        with self.assertRaises(ValidationError):
            transition(self.order, WorkOrder.Status.ASSIGNED, self.employee, "N" * 501)
        self.order.refresh_from_db()
        self.assertEqual(self.order.status, WorkOrder.Status.DRAFT)

    def test_assigned_cancellation_clears_assignment_metadata(self):
        transition(self.order, WorkOrder.Status.ASSIGNED, self.employee)
        transition(self.order, WorkOrder.Status.CANCELLED, self.employee)
        self.order.refresh_from_db()
        self.assertEqual(self.order.status, WorkOrder.Status.CANCELLED)
        self.assertIsNone(self.order.assignee)
        self.assertIsNone(self.order.assigned_at)

    def test_shelving_preserves_the_original_assignment_timestamp(self):
        transition(self.order, WorkOrder.Status.ASSIGNED, self.employee)
        self.order.refresh_from_db()
        original_assigned_at = self.order.assigned_at
        transition(self.order, WorkOrder.Status.IN_PROGRESS, self.employee)
        transition(self.order, WorkOrder.Status.ASSIGNED, self.employee)
        self.order.refresh_from_db()
        self.assertEqual(self.order.assigned_at, original_assigned_at)
    def test_due_date_urgency_is_read_time_and_respects_status(self):
        today = date(2026, 9, 23)
        self.order.due_date = today - timedelta(days=1)
        self.assertEqual(due_date_urgency(self.order, today), "Overdue")
        self.assertEqual(due_date_badge(self.order, today), "Overdue")
        self.order.due_date = today
        self.assertEqual(due_date_urgency(self.order, today), "DueToday")
        self.assertEqual(due_date_badge(self.order, today), "Due Today")
        self.order.due_date = today + timedelta(days=1)
        self.assertIsNone(due_date_urgency(self.order, today))
        self.assertEqual(due_date_badge(self.order, today), "On Track")
        self.order.due_date = today - timedelta(days=1)
        self.order.status = WorkOrder.Status.COMPLETE
        self.assertIsNone(due_date_urgency(self.order, today))
        self.assertEqual(due_date_badge(self.order, today), "On Track")
    def test_urgency_badge_is_absent_when_due_date_is_missing(self):
        self.assertIsNone(due_date_badge(self.order, date(2026, 9, 23)))
    @patch("workorders.services.timezone.now", return_value=datetime(2026, 9, 23, 4, 30, tzinfo=datetime_timezone.utc))
    def test_today_uses_the_chicago_calendar_boundary(self, _now):
        self.assertEqual(chicago_today(), date(2026, 9, 22))

@override_settings(ENABLE_DEMO_LOGIN=True)
class WorkOrderWebTests(TestCase):
    def setUp(self):
        self.creator = Employee.objects.create(username="creator", first_name="Homer", last_name="Simpson")
        self.creator.roles.add(Role.objects.create(name="Facility Lead", can_create_work_order=True))
        self.assignee = Employee.objects.create(username="worker", first_name="Ned", last_name="Flanders")

    def login_as(self, employee):
        self.client.post(reverse("login"), {"username": employee.username})

    def test_health_is_json(self):
        response = self.client.get(reverse("health"))
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "Healthy")

    @patch("workorders.views.connection.ensure_connection", side_effect=OSError("database is down"))
    def test_health_reports_database_failure(self, _ensure_connection):
        response = self.client.get(reverse("health"))
        self.assertEqual(response.status_code, 503)
        self.assertEqual(response.json()["database"], "unavailable")

    @override_settings(ENABLE_DEMO_LOGIN=False)
    def test_demo_login_is_disabled_without_explicit_opt_in(self):
        response = self.client.get(reverse("login"))
        self.assertEqual(response.status_code, 503)
        self.assertNotContains(response, "HOMER SIMPSON", status_code=503)

    def test_work_order_list_and_detail_require_an_active_session(self):
        order = WorkOrder.objects.create(title="Private order")
        self.assertRedirects(self.client.get(reverse("work_order_list")), reverse("login"))
        self.assertRedirects(self.client.get(reverse("work_order_detail", args=[order.pk])), reverse("login"))

    def test_admin_cannot_edit_lifecycle_or_mutate_history(self):
        work_order_admin = admin.site._registry[WorkOrder]
        event_admin = admin.site._registry[WorkOrderEvent]
        self.assertIn("status", work_order_admin.get_readonly_fields(None))
        self.assertIn("assignee", work_order_admin.get_readonly_fields(None))
        self.assertIn(Role, admin.site._registry)
        self.assertFalse(event_admin.has_add_permission(None))
        self.assertFalse(event_admin.has_delete_permission(None))
        attachment_admin = admin.site._registry[WorkOrderAttachment]
        self.assertFalse(attachment_admin.has_add_permission(None))
        self.assertFalse(attachment_admin.has_delete_permission(None))
    def test_create_search_filter_detail_and_status_transition(self):
        self.login_as(self.creator)
        response = self.client.post(reverse("work_order_create"), {"title": "Replace light", "description": "Hallway", "instructions": "Use ladder", "room_number": "101", "assignee": self.assignee.pk, "due_date": ""})
        order = WorkOrder.objects.get(title="Replace light")
        self.assertEqual(response.status_code, 302)
        self.assertIn(order.number, self.client.get(reverse("work_order_list") + "?q=WO-").content.decode())
        self.assertEqual(self.client.get(reverse("work_order_detail", args=[order.pk])).status_code, 200)
        response = self.client.post(reverse("work_order_transition", args=[order.pk]), {"status": WorkOrder.Status.ASSIGNED}, follow=True)
        order.refresh_from_db()
        self.assertEqual(order.status, WorkOrder.Status.ASSIGNED)
        self.assertEqual(order.events.count(), 1)
        self.assertContains(response, f"Work order {order.number} moved to Assigned.")
    def test_list_search_matches_room_and_title(self):
        self.login_as(self.creator)
        WorkOrder.objects.create(title="Inspect boiler", room_number="B-2")
        response = self.client.get(reverse("work_order_list"), {"q": "B-2"})
        self.assertContains(response, "Inspect boiler")
        self.assertNotContains(response, "Repair sink")
    def test_search_filters_by_creator_assignee_and_assigned_to_me_and_sorts(self):
        self.login_as(self.creator)
        mine = WorkOrder.objects.create(title="Zeta", creator=self.assignee, assignee=self.creator)
        other = WorkOrder.objects.create(title="Alpha", creator=self.creator, assignee=self.assignee)
        response = self.client.get(reverse("work_order_list"), {"creator": self.assignee.pk})
        self.assertContains(response, mine.number)
        self.assertNotContains(response, other.number)
        response = self.client.get(reverse("work_order_list"), {"assignee": self.assignee.pk})
        self.assertContains(response, other.number)
        self.assertNotContains(response, mine.number)
        response = self.client.get(reverse("work_order_list"), {"assigned_to_me": "1"})
        self.assertContains(response, mine.number)
        self.assertNotContains(response, other.number)
        response = self.client.get(reverse("work_order_list"), {"sort": "Title", "direction": "asc"})
        self.assertLess(response.content.index(b"Alpha"), response.content.index(b"Zeta"))
    def test_room_number_accepts_900_characters_and_rejects_901(self):
        self.login_as(self.creator)
        room = "R" * 900
        response = self.client.post(reverse("work_order_create"), {"title": "Long room", "room_number": room, "due_date": ""})
        self.assertEqual(response.status_code, 302)
        self.assertEqual(WorkOrder.objects.get(title="Long room").room_number, room)
        response = self.client.post(reverse("work_order_create"), {"title": "Too long room", "room_number": room + "X", "due_date": ""})
        self.assertEqual(response.status_code, 200)
        self.assertContains(response, "Ensure this value has at most 900 characters")
        self.assertFalse(WorkOrder.objects.filter(title="Too long room").exists())

    def test_login_session_survives_navigation_and_logout_clears_it(self):
        response = self.client.post(reverse("login"), {"username": self.creator.username})
        self.assertEqual(response.status_code, 302)
        self.assertContains(self.client.get(reverse("work_order_list")), "HOMER SIMPSON")
        self.assertContains(self.client.get(reverse("work_order_list")), "New work order")
        self.client.post(reverse("logout"))
        self.assertRedirects(self.client.get(reverse("work_order_list")), reverse("login"))
        self.assertNotContains(self.client.get(reverse("login")), "New work order")

    def test_login_picker_formats_names_and_lovejoy_shortcut_authenticates_by_username(self):
        lovejoy = Employee.objects.create(username="tlovejoy", first_name="Timothy", last_name="Lovejoy")
        response = self.client.get(reverse("login"))
        self.assertContains(response, "-- Select a parishioner or staff member --")
        self.assertContains(response, "HOMER SIMPSON")
        self.assertContains(response, "NED FLANDERS")
        response = self.client.post(reverse("login"), {"username": "tlovejoy"})
        self.assertEqual(response.status_code, 302)
        self.assertEqual(self.client.session["workorders_employee_id"], lovejoy.pk)

    def test_login_rejects_unknown_and_inactive_employee(self):
        Employee.objects.filter(pk=self.creator.pk).update(active=False)
        response = self.client.post(reverse("login"), {"username": self.creator.username})
        self.assertContains(response, "Select a valid employee")
        self.assertNotIn("workorders_employee_id", self.client.session)
        response = self.client.post(reverse("login"), {"username": "unknown"})
        self.assertContains(response, "Select a valid employee")

    def test_creator_role_controls_create_navigation_and_server_access(self):
        response = self.client.get(reverse("work_order_create"))
        self.assertEqual(response.status_code, 302)
        self.login_as(self.assignee)
        self.assertNotContains(self.client.get(reverse("work_order_list")), "New work order")
        self.assertEqual(self.client.get(reverse("work_order_create")).status_code, 403)

    def test_state_actions_require_correct_actor_and_begin_state(self):
        order = WorkOrder.objects.create(title="Secure order", creator=self.creator, assignee=self.assignee)
        self.login_as(self.assignee)
        self.client.post(reverse("work_order_transition", args=[order.pk]), {"status": WorkOrder.Status.ASSIGNED})
        order.refresh_from_db()
        self.assertEqual(order.status, WorkOrder.Status.DRAFT)
        self.assertEqual(available_transitions(order, self.assignee), [])
        self.login_as(self.creator)
        self.assertEqual(available_transitions(order, self.creator), [WorkOrder.Status.ASSIGNED, WorkOrder.Status.CANCELLED])
        transition(order, WorkOrder.Status.ASSIGNED, self.creator)
        order.refresh_from_db()
        self.assertEqual(available_transitions(order, self.assignee), [WorkOrder.Status.IN_PROGRESS])
        with self.assertRaises(ValidationError):
            transition(order, WorkOrder.Status.IN_PROGRESS, self.creator)
        transition(order, WorkOrder.Status.IN_PROGRESS, self.assignee)
        order.refresh_from_db()
        self.assertEqual(available_transitions(order, self.creator), [])
        self.assertEqual(available_transitions(order, self.assignee), [WorkOrder.Status.ASSIGNED, WorkOrder.Status.COMPLETE])

    def test_attachment_metadata_is_saved_with_uploader_and_shown_on_detail(self):
        self.login_as(self.creator)
        order = WorkOrder.objects.create(title="Order with metadata", creator=self.creator)
        response = self.client.post(reverse("work_order_attachment_create", args=[order.pk]), {
            "file_name": "damage-photo.jpg",
            "content_type": "image/jpeg",
            "file_size": "2048",
        }, follow=True)
        attachment = WorkOrderAttachment.objects.get(work_order=order)
        self.assertContains(response, 'data-testid="AttachmentsSection"')
        self.assertContains(response, 'data-testid="AttachmentFileName">damage-photo.jpg')
        self.assertContains(response, 'data-testid="AttachmentContentType">image/jpeg')
        self.assertContains(response, 'data-testid="AttachmentFileSize">2048')
        self.assertContains(response, 'data-testid="AttachmentUploadedBy">Homer Simpson')
        self.assertEqual(attachment.uploaded_by, self.creator)
        self.assertEqual(attachment.file_size, 2048)
        self.assertEqual(
            {field.name for field in WorkOrderAttachment._meta.fields},
            {"id", "work_order", "file_name", "content_type", "file_size", "uploaded_by", "uploaded_date"},
        )

    def test_attachment_metadata_requires_login_and_nonblank_file_name(self):
        order = WorkOrder.objects.create(title="Protected attachment")
        url = reverse("work_order_attachment_create", args=[order.pk])
        response = self.client.post(url, {"file_name": "example.pdf", "file_size": "100"})
        self.assertEqual(response.status_code, 302)
        self.login_as(self.creator)
        response = self.client.post(url, {"file_name": "   ", "file_size": "100"})
        self.assertEqual(response.status_code, 200)
        self.assertContains(response, "This field is required.")
        response = self.client.post(url, {"file_name": "negative.pdf", "file_size": "-1"})
        self.assertEqual(response.status_code, 200)
        self.assertContains(response, "Ensure this value is greater than or equal to 0.")
        self.assertFalse(WorkOrderAttachment.objects.filter(work_order=order).exists())
    @patch("workorders.views.chicago_today", return_value=date(2026, 9, 23))
    def test_overdue_filter_excludes_today_and_closed_orders(self, _today):
        self.login_as(self.creator)
        old_date = date(2026, 9, 22)
        overdue = WorkOrder.objects.create(title="Open overdue", due_date=old_date)
        today = WorkOrder.objects.create(title="Due today", due_date=date(2026, 9, 23))
        completed = WorkOrder.objects.create(title="Closed overdue", due_date=old_date, status=WorkOrder.Status.COMPLETE)
        response = self.client.get(reverse("work_order_list"), {"overdue": "1"})
        self.assertContains(response, overdue.title)
        self.assertNotContains(response, today.title)
        self.assertNotContains(response, completed.title)
        self.assertContains(response, 'class="due-date-overdue"')
    @patch("workorders.views.chicago_today", return_value=date(2026, 9, 23))
    def test_list_displays_today_overdue_and_on_track_badges(self, _today):
        self.login_as(self.creator)
        WorkOrder.objects.create(title="Today", due_date=date(2026, 9, 23))
        WorkOrder.objects.create(title="Overdue", due_date=date(2026, 9, 22))
        WorkOrder.objects.create(title="Future", due_date=date(2026, 9, 24))
        response = self.client.get(reverse("work_order_list"))
        self.assertContains(response, "Due Today")
        self.assertContains(response, "Overdue")
        self.assertContains(response, "On Track")
        self.assertContains(response, "due-date-today")
        self.assertContains(response, "due-date-overdue")
    def test_list_leaves_due_cell_blank_and_has_no_badge_without_date(self):
        self.login_as(self.creator)
        order = WorkOrder.objects.create(title="No due date")
        response = self.client.get(reverse("work_order_list"))
        self.assertContains(response, f'data-testid="DueDateCell{order.number}" class=""></span>')
        self.assertNotContains(response, f'UrgencyBadge{order.number}')

    def test_operational_api_health_is_lightweight_and_versioned(self):
        with patch("workorders.operations.connection.ensure_connection", side_effect=OSError("database down")):
            response = self.client.get("/api/health/")
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "Healthy")
        self.assertIn("currentTimeUtc", response.json())
        self.assertEqual(self.client.get("/api/v1.0/health/").json()["status"], "Healthy")

    def test_detailed_health_reports_database_component(self):
        response = self.client.get("/api/health/detailed/")
        self.assertEqual(response.status_code, 200)
        self.assertIn(response.json()["overallStatus"], {"Healthy", "Unhealthy"})
        self.assertEqual(response.json()["components"][0]["name"], "database")

    def test_feature_flags_diagnostics_and_version_contracts(self):
        flags = self.client.get("/api/features/flags/")
        self.assertEqual(flags.json(), {"SampleFeatureA": True, "SampleFeatureB": False})
        self.assertEqual(self.client.get("/api/v1.0/features/flags/").json(), flags.json())
        diagnostics = self.client.get("/api/diagnostics/").json()
        self.assertEqual(diagnostics["featureFlags"], {"sampleFeatureA": False, "sampleFeatureB": False})
        version = self.client.get("/api/version/").json()
        self.assertIn("frameworkDescription", version)
        self.assertEqual(version["informationalVersion"], "unknown")

    @patch.dict("os.environ", {"TEST_ENV_STATUS_SECRET": "must never leak"})
    def test_environment_status_redacts_selected_values(self):
        response = self.client.get("/api/status/environment/")
        self.assertEqual(response.status_code, 200)
        self.assertIn("TEST_ENV_STATUS_SECRET", response.json()["environmentVariableNames"])
        self.assertEqual(response.json()["environmentVariables"]["TEST_ENV_STATUS_SECRET"], "[REDACTED]")
        self.assertNotIn("must never leak", response.content.decode())

    def test_echo_redacts_sensitive_headers_and_metrics_reports_fields(self):
        response = self.client.get("/api/echo/?probe=yes", HTTP_AUTHORIZATION="Bearer private", HTTP_X_API_KEY="secret")
        self.assertEqual(response.json()["queryString"], "?probe=yes")
        headers = {key.lower(): value for key, value in response.json()["headers"].items()}
        self.assertEqual(headers["authorization"], "[REDACTED]")
        self.assertEqual(headers["x-api-key"], "[REDACTED]")
        metrics = self.client.get("/api/metrics/summary/").json()
        self.assertGreaterEqual(metrics["totalRequestsServed"], 2)
        self.assertIn("workingSetBytes", metrics)
        self.assertIn("gcGen2Collections", metrics)

    def test_operational_api_honors_conditional_get(self):
        response = self.client.get("/api/features/flags/")
        unchanged = self.client.get("/api/features/flags/", HTTP_IF_NONE_MATCH=response["ETag"])
        self.assertEqual(unchanged.status_code, 304)
        strong_validator = response["ETag"].removeprefix("W/")
        self.assertEqual(self.client.get("/api/features/flags/", HTTP_IF_NONE_MATCH=strong_validator).status_code, 304)
        detailed = self.client.get("/api/health/detailed/")
        unchanged_detailed = self.client.get("/api/health/detailed/", HTTP_IF_NONE_MATCH=detailed["ETag"])
        self.assertEqual(unchanged_detailed.status_code, 304)
