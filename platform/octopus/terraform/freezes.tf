# Deployment freeze prod-weekend-freeze (§7.2, ADR-D5): project workorders, environment prod, every weekend.
#
# A per-project freeze is used: multi-project freezes need an Enterprise licence from 2025.2.7631, while project
# freezes are available to all customers and support recurring schedules
# (https://octopus.com/docs/deployments/deployment-freezes,
#  https://octopus.com/docs/deployments/deployment-freezes/project-deployment-freezes). This resolves the
# [VERIFY recurring support] of §7.2 for provider 1.20.0 (recurring_schedule on this resource).
# Override: users with ProjectEdit scoped to prod may override and must enter a reason in the override dialog
# (Release Managers; see teams.tf for the approver caveat). Freezes also block automatic lifecycle promotions,
# which never target prod here.
# Recurrence semantics: the first window (Saturday 00:00 to Monday 00:00) repeats weekly on Saturday
# [VERIFY that the window keeps its 48-hour length on each occurrence].

resource "octopusdeploy_project_deployment_freeze" "prod_weekend" {
  name            = "prod-weekend-freeze"
  owner_id        = octopusdeploy_project.workorders.id
  environment_ids = [octopusdeploy_environment.this["prod"].id]
  start           = var.prod_freeze_first_window.start
  end             = var.prod_freeze_first_window.end

  recurring_schedule = {
    type         = "Weekly"
    unit         = 1
    days_of_week = ["Saturday"]
    end_type     = "Never"
  }
}
