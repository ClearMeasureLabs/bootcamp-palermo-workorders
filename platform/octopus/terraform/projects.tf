# Project group, both version-controlled projects and the scheduled runbook triggers (§7.2, ADR-D7).
#
# Order of first use: commit .octopus/workorders and .octopus/workorders-infrastructure to main (protected) first,
# then apply. Octopus then reads the existing OCL instead of committing an initial skeleton to a protected branch
# [VERIFY conversion behaviour with pre-existing OCL].
# Deployment settings, process, runbooks and non-sensitive variables live in Git; project-level settings that are
# stored in the database (lifecycle, group, included library variable sets, Git settings) live here.

resource "octopusdeploy_project_group" "work_orders" {
  name        = "Work Orders"
  description = "Work Orders delivery platform: application delivery and environment lifecycle (platform-design §7.2)."
}

# Stored Git credential: looked up by name, never managed. Its repository restriction should be narrowed to the
# environment repo (R3); the Argo CD step also selects it by that restriction.
data "octopusdeploy_git_credentials" "stored" {
  name = var.stored_git_credential_name
  take = 10

  lifecycle {
    postcondition {
      condition     = length([for c in self.git_credentials : c if c.name == var.stored_git_credential_name]) == 1
      error_message = "Expected exactly one Git credential named '${var.stored_git_credential_name}'. It is stored by the user and never created here."
    }
  }
}

locals {
  stored_git_credential_id = one([for c in data.octopusdeploy_git_credentials.stored.git_credentials : c.id if c.name == var.stored_git_credential_name])
}

resource "octopusdeploy_project" "workorders" {
  name                              = "workorders"
  slug                              = "workorders"
  description                       = "Work Orders application delivery: DbUp migration, Argo CD image-tag pin, verification and gates. Releases come from Codefresh workorders/release (§7.7)."
  project_group_id                  = octopusdeploy_project_group.work_orders.id
  lifecycle_id                      = octopusdeploy_lifecycle.workorders_standard.id
  tenanted_deployment_participation = "Untenanted"
  default_guided_failure_mode       = "EnvironmentDefault"
  included_library_variable_sets    = [octopusdeploy_library_variable_set.workorders_environment.id]

  git_library_persistence_settings {
    url                = var.env_repo_url
    git_credential_id  = local.stored_git_credential_id
    base_path          = ".octopus/workorders"
    default_branch     = "main"
    protected_branches = ["main"]
  }

  lifecycle {
    precondition {
      condition     = length(setintersection(toset(local.stored_library_variable_set_ids), toset([octopusdeploy_library_variable_set.workorders_environment.id]))) == 0
      error_message = "Stored library variable sets are included in no project (§5.3, ADR-C10)."
    }
  }
}

resource "octopusdeploy_project" "workorders_infrastructure" {
  name                              = "workorders-infrastructure"
  slug                              = "workorders-infrastructure"
  description                       = "Runbooks only: env-plan, env-apply, env-destroy (infra-nonprod), rotate-sql-passwords, provisioner-credential-check (ADR-D10)."
  project_group_id                  = octopusdeploy_project_group.work_orders.id
  lifecycle_id                      = octopusdeploy_lifecycle.workorders_infrastructure.id
  tenanted_deployment_participation = "Untenanted"
  default_guided_failure_mode       = "EnvironmentDefault"
  included_library_variable_sets    = [octopusdeploy_library_variable_set.workorders_infrastructure.id]

  git_library_persistence_settings {
    url                = var.env_repo_url
    git_credential_id  = local.stored_git_credential_id
    base_path          = ".octopus/workorders-infrastructure"
    default_branch     = "main"
    protected_branches = ["main"]
  }

  lifecycle {
    precondition {
      condition     = length(setintersection(toset(local.stored_library_variable_set_ids), toset([octopusdeploy_library_variable_set.workorders_infrastructure.id]))) == 0
      error_message = "Stored library variable sets are included in no project (§5.3, ADR-C10)."
    }
  }
}

# Scheduled runbook triggers (§7.2). Triggers are not stored in Git (E26) and run config-as-code runbooks from the
# latest commit on the default branch (https://octopus.com/docs/runbooks/config-as-code-runbooks).
# Octopus cron has six fields (seconds first). runbook_id for a config-as-code runbook: the slug is assumed
# [VERIFY the ID form Octopus expects for Git-stored runbooks].

resource "octopusdeploy_project_scheduled_trigger" "rotate_sql_passwords_monthly" {
  count = var.runbook_triggers_enabled ? 1 : 0

  name        = "rotate-sql-passwords-monthly"
  description = "Monthly rotation of the interim SQL passwords in infra-nonprod and infra-prod."
  project_id  = octopusdeploy_project.workorders_infrastructure.id
  space_id    = var.octopus_space_id
  timezone    = var.runbook_trigger_timezone

  cron_expression_schedule {
    cron_expression = "0 0 3 1 * *"
  }

  run_runbook_action {
    runbook_id = "rotate-sql-passwords"
    target_environment_ids = [
      octopusdeploy_environment.this["infra-nonprod"].id,
      octopusdeploy_environment.this["infra-prod"].id,
    ]
  }
}

resource "octopusdeploy_project_scheduled_trigger" "provisioner_credential_check_daily" {
  count = var.runbook_triggers_enabled ? 1 : 0

  name        = "provisioner-credential-check-daily"
  description = "Daily sign-in smoke and expiry warning for the stored provisioner secret (infra-nonprod, phases 1-2)."
  project_id  = octopusdeploy_project.workorders_infrastructure.id
  space_id    = var.octopus_space_id
  timezone    = var.runbook_trigger_timezone

  cron_expression_schedule {
    cron_expression = "0 0 6 * * *"
  }

  run_runbook_action {
    runbook_id             = "provisioner-credential-check"
    target_environment_ids = [octopusdeploy_environment.this["infra-nonprod"].id]
  }
}
