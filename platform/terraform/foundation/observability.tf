# Shared Log Analytics workspace log-workorders (ADR-D15) and the security alerts that give the
# runbooks their audit evidence. Per-environment App Insights and the SLO alert live in
# terraform/environment/monitoring.tf.

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-workorders"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_retention_days

  # Ingestion comes from Azure Monitor diagnostic settings and workspace-based App Insights,
  # neither of which uses workspace shared keys, so shared-key access stays off
  # [VERIFY no legacy agent is added later].
  local_authentication_enabled = false

  tags = var.tags
}

resource "azurerm_monitor_action_group" "security" {
  name                = "ag-workorders-security"
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  short_name          = "wo-security"

  dynamic "email_receiver" {
    for_each = var.security_alert_email_receivers

    content {
      name                    = email_receiver.key
      email_address           = email_receiver.value
      use_common_alert_schema = true
    }
  }

  tags = var.tags
}

# A lock was lifted: expected only inside a break-glass or change window, and the alert is the
# evidence that the window was opened.
resource "azurerm_monitor_activity_log_alert" "lock_deleted" {
  name                = "workorders-lock-deleted"
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  location            = "global"
  scopes              = [data.azurerm_subscription.current.id]
  description         = "A management lock was deleted in the workorders subscription (docs/runbooks/break-glass.md)."

  criteria {
    category       = "Administrative"
    operation_name = "Microsoft.Authorization/locks/delete"
  }

  action {
    action_group_id = azurerm_monitor_action_group.security.id
  }

  tags = var.tags
}

# Role assignments change only through this layer; any other write is a finding.
resource "azurerm_monitor_activity_log_alert" "role_assignment_written" {
  name                = "workorders-role-assignment-written"
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  location            = "global"
  scopes              = [for rg in local.resource_group_names : azurerm_resource_group.this[rg].id]
  description         = "A role assignment was created or changed in a workorders resource group. Expected only during a terraform/foundation apply."

  criteria {
    category       = "Administrative"
    operation_name = "Microsoft.Authorization/roleAssignments/write"
    statuses       = ["Succeeded"]
  }

  action {
    action_group_id = azurerm_monitor_action_group.security.id
  }

  tags = var.tags
}

# --- Stored provisioner sign-ins (ADR-C10 recommendation 4) --------------------------------------
# Every sign-in of the stored client-secret principal raises an informational alert. Expected
# only while an Octopus workorders-infrastructure runbook runs in infra-nonprod (phases 1-2).

resource "azurerm_monitor_aad_diagnostic_setting" "signin" {
  count = var.entra_signin_logs_enabled ? 1 : 0

  name                       = "workorders-sp-signins-to-log-workorders"
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id

  enabled_log {
    category = "ServicePrincipalSignInLogs"
  }
}

resource "azurerm_monitor_scheduled_query_rules_alert_v2" "provisioner_signin" {
  count = var.entra_signin_logs_enabled && var.provisioner_object_id != null ? 1 : 0

  name                 = "workorders-provisioner-signin"
  resource_group_name  = azurerm_resource_group.this[local.rg_shared].name
  location             = var.location
  scopes               = [azurerm_log_analytics_workspace.this.id]
  severity             = 3
  evaluation_frequency = "PT15M"
  window_duration      = "PT15M"
  description          = "The stored Azure Runtime Provisioner signed in. Compare with Octopus task history for workorders-infrastructure runbooks in infra-nonprod."

  criteria {
    query                   = <<-KQL
      AADServicePrincipalSignInLogs
      | where ServicePrincipalId == "${coalesce(var.provisioner_object_id, "none")}"
      | project TimeGenerated, IPAddress, ResultType, ResourceDisplayName, AppId
    KQL
    time_aggregation_method = "Count"
    operator                = "GreaterThan"
    threshold               = 0

    failing_periods {
      minimum_failing_periods_to_trigger_alert = 1
      number_of_evaluation_periods             = 1
    }
  }

  action {
    action_groups = [azurerm_monitor_action_group.security.id]
  }

  depends_on = [azurerm_monitor_aad_diagnostic_setting.signin]

  tags = var.tags
}
