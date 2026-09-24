# Observability per environment (ADR-D15): one workspace-based App Insights resource per
# environment in the shared log-workorders workspace, and the fast-burn SLO alert.
# The app exports to Azure Monitor when ApplicationInsights:ConnectionString is set
# (app:src/ChurchBulletin.ServiceDefaults/Extensions.cs); the connection string reaches the pods
# as Key Vault secret workorders-appinsights-connection-string (data-services.tf) through ESO.
# No OTel collector or Prometheus until phase 6.

resource "azurerm_application_insights" "env" {
  for_each = toset(local.envs)

  name                = "appi-workorders-${each.key}"
  location            = var.location
  resource_group_name = local.rg_env[each.key]
  workspace_id        = var.log_analytics_workspace_id
  application_type    = "web"

  # The app authenticates telemetry with the connection string only; Entra-only ingestion needs
  # an app change (a credential in UseAzureMonitor), so local authentication stays on.
  local_authentication_enabled = true
  ip_masking_enabled           = true

  tags = var.tags
}

resource "azurerm_monitor_action_group" "oncall" {
  name                = "ag-workorders-${var.cluster}-oncall"
  resource_group_name = local.rg_aks
  short_name          = var.cluster == "prod" ? "wo-oncall-pr" : "wo-oncall-np"

  dynamic "email_receiver" {
    for_each = var.oncall_email_receivers

    content {
      name                    = email_receiver.key
      email_address           = email_receiver.value
      use_common_alert_schema = true
    }
  }

  tags = var.tags
}

# --- Fast-burn SLO alert (ADR-D15) -----------------------------------------------------------------
# SLO: 99.5 % of ui-server requests complete without a 5xx over 28 days. Error budget 0.5 %.
# Fast burn: 2 % of the 28-day budget spent within one hour, i.e. a burn rate of
# 0.02 * (28 * 24) = 13.44, confirmed over a 5-minute window so the alert also clears quickly.
# Health, liveness and version probes are excluded so probe traffic neither dilutes nor
# triggers the rate. A minimum of 50 requests in the hour keeps a single failed request on an
# idle environment from paging.
# Response: docs/runbooks/slo-fast-burn.md.

locals {
  slo_fast_burn_query = <<-KQL
    let slo = 0.995;
    let burnThreshold = 13.44;
    let minRequests = 50;
    let excludedPaths = dynamic(["/alive", "/health", "/_healthcheck", "/_healthcheck/detailed", "/_version"]);
    let scoped = requests
        | where timestamp > ago(1h)
        | extend path = tostring(parse_url(url).Path)
        | where path !in (excludedPaths);
    let longWindow = scoped
        | summarize total = count(), failed = countif(toint(resultCode) >= 500)
        | extend window = "1h";
    let shortWindow = scoped
        | where timestamp > ago(5m)
        | summarize total = count(), failed = countif(toint(resultCode) >= 500)
        | extend window = "5m";
    union longWindow, shortWindow
    | extend burnRate = iff(total == 0, 0.0, (todouble(failed) / todouble(total)) / (1.0 - slo))
    | summarize longBurn = maxif(burnRate, window == "1h"), shortBurn = maxif(burnRate, window == "5m"), requestsLastHour = maxif(total, window == "1h")
    | where requestsLastHour >= minRequests and longBurn >= burnThreshold and shortBurn >= burnThreshold
  KQL
}

resource "azurerm_monitor_scheduled_query_rules_alert_v2" "slo_fast_burn" {
  for_each = toset(local.envs)

  name                 = "slo-fast-burn-ui-server-${each.key}"
  display_name         = "workorders ${each.key}: ui-server error budget fast burn"
  description          = "More than 2 % of the 28-day error budget (99.5 % non-5xx) burned in the last hour, confirmed over 5 minutes. Runbook: docs/runbooks/slo-fast-burn.md in ${var.env_repo_url}."
  resource_group_name  = local.rg_env[each.key]
  location             = var.location
  scopes               = [azurerm_application_insights.env[each.key].id]
  enabled              = var.slo_alert_enabled
  severity             = each.key == "prod" ? 1 : 3
  evaluation_frequency = "PT5M"
  window_duration      = "PT1H"

  criteria {
    query                   = local.slo_fast_burn_query
    time_aggregation_method = "Count"
    operator                = "GreaterThan"
    threshold               = 0

    failing_periods {
      minimum_failing_periods_to_trigger_alert = 1
      number_of_evaluation_periods             = 1
    }
  }

  auto_mitigation_enabled = true

  action {
    action_groups = [azurerm_monitor_action_group.oncall.id]
    custom_properties = {
      environment = each.key
      runbook     = "docs/runbooks/slo-fast-burn.md"
    }
  }

  tags = var.tags
}
