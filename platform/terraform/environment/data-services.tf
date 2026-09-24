# SQL and Key Vault per environment, the platform vault per cluster, and the secrets this layer
# writes (§7.8, ADR-D9).
#
# Secrets and state:
#   - Generated passwords come from ephemeral random_password resources and reach Azure through
#     write-only arguments (value_wo, administrator_login_password_wo), so they are never stored
#     in the plan or the state. value_wo_version stays 1: Terraform writes each secret once and
#     never reverts a rotation done by rotate-sql-passwords or a person
#     (docs/runbooks/credential-rotation.md).
#   - The SQL server admin password is generated and discarded: nobody knows it. Administration
#     goes through the Entra admin group <sql-admins-{class}>. SQL authentication stays enabled
#     only for the two interim contained users until WI-05 (ADR-D9); the foundation audits it.
#   - The contained users (workorders_migrator, workorders_acceptance and the workload-identity
#     users of id-workorders-<env>-app) are created by configure-db-principals-<env> in env-apply,
#     which reads the passwords from the vault. No Terraform provider for SQL users is used.
#
# Network: SQL has no public endpoint; the Octopus Kubernetes worker in the cluster reaches it
# through the private endpoint. Vault data planes stay publicly reachable (behind the firewall
# when key_vault_allowed_ip_ranges is set) because env-apply writes secrets from Octopus Cloud
# dynamic workers; workloads use the private endpoints.

# --- Generated values (ephemeral: never in plan or state) -----------------------------------------

ephemeral "random_password" "sql_admin" {
  for_each = toset(local.envs)

  length           = 40
  override_special = "!#%*-_=+"
  min_special      = 2
}

ephemeral "random_password" "sql_migrator" {
  for_each = toset(local.envs)

  length           = 40
  override_special = "!#%*-_=+"
  min_special      = 2
}

ephemeral "random_password" "sql_acceptance" {
  for_each = toset([for e in local.envs : e if e == "tdd"])

  length           = 40
  override_special = "!#%*-_=+"
  min_special      = 2
}

ephemeral "random_password" "api_validation_key" {
  for_each = toset(local.envs)

  length  = 48
  special = false
}

# --- Azure SQL ------------------------------------------------------------------------------------

resource "azurerm_mssql_server" "env" {
  for_each = toset(local.envs)

  name                          = var.sql_server_names[each.key]
  resource_group_name           = local.rg_env[each.key]
  location                      = var.location
  version                       = "12.0"
  minimum_tls_version           = "1.2"
  public_network_access_enabled = false

  administrator_login                     = "workorders_sqladmin"
  administrator_login_password_wo         = ephemeral.random_password.sql_admin[each.key].result
  administrator_login_password_wo_version = 1

  azuread_administrator {
    login_username = var.sql_admin_group.display_name
    object_id      = var.sql_admin_group.object_id
    tenant_id      = var.tenant_id
    # true after WI-05 removes the last password user.
    azuread_authentication_only = false
  }

  tags = var.tags
}

resource "azurerm_mssql_database" "env" {
  for_each = toset(local.envs)

  name                 = var.sql_database_names[each.key]
  server_id            = azurerm_mssql_server.env[each.key].id
  sku_name             = var.sql_databases[each.key].sku_name
  storage_account_type = var.sql_databases[each.key].backup_storage_redundancy

  # Point-in-time restore window (docs/runbooks/database-restore-pitr.md): 7 days on Basic,
  # up to 35 days on Standard and above.
  short_term_retention_policy {
    retention_days           = var.sql_databases[each.key].pitr_days
    backup_interval_in_hours = 12
  }

  dynamic "long_term_retention_policy" {
    for_each = anytrue([
      var.sql_databases[each.key].ltr_weekly != null,
      var.sql_databases[each.key].ltr_monthly != null,
      var.sql_databases[each.key].ltr_yearly != null,
    ]) ? [1] : []

    content {
      weekly_retention  = var.sql_databases[each.key].ltr_weekly
      monthly_retention = var.sql_databases[each.key].ltr_monthly
      yearly_retention  = var.sql_databases[each.key].ltr_yearly
      week_of_year      = var.sql_databases[each.key].ltr_week_of_year
    }
  }

  tags = var.tags
}

resource "azurerm_private_endpoint" "sql" {
  for_each = toset(local.envs)

  name                = "pe-${var.sql_server_names[each.key]}"
  location            = var.location
  resource_group_name = local.rg_env[each.key]
  subnet_id           = var.private_endpoints_subnet_id

  private_service_connection {
    name                           = "sqlServer"
    private_connection_resource_id = azurerm_mssql_server.env[each.key].id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_ids.sql]
  }

  tags = var.tags
}

# SQL audit (logins, permission changes, DDL) to log-workorders: evidence for the restore and
# rotation runbooks.
resource "azurerm_mssql_server_extended_auditing_policy" "env" {
  for_each = toset(local.envs)

  server_id              = azurerm_mssql_server.env[each.key].id
  log_monitoring_enabled = true
}

resource "azurerm_monitor_diagnostic_setting" "sql_audit" {
  for_each = toset(local.envs)

  name                       = "sql-audit-to-log-workorders"
  target_resource_id         = "${azurerm_mssql_server.env[each.key].id}/databases/master"
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "SQLSecurityAuditEvents"
  }

  depends_on = [azurerm_mssql_server_extended_auditing_policy.env]
}

# --- Key Vaults (RBAC permission model only; the foundation denies access-policy vaults) ---------

locals {
  vaults = merge(
    { for e in local.envs : e => { name = var.key_vault_names[e], resource_group = local.rg_env[e] } },
    { platform = { name = var.platform_key_vault_name, resource_group = local.rg_aks } },
  )
}

resource "azurerm_key_vault" "this" {
  for_each = local.vaults

  name                = each.value.name
  location            = var.location
  resource_group_name = each.value.resource_group
  tenant_id           = var.tenant_id
  sku_name            = "standard"

  rbac_authorization_enabled = true
  purge_protection_enabled   = true
  soft_delete_retention_days = 90

  public_network_access_enabled = true

  network_acls {
    default_action = length(var.key_vault_allowed_ip_ranges) > 0 ? "Deny" : "Allow"
    bypass         = "AzureServices"
    ip_rules       = var.key_vault_allowed_ip_ranges
  }

  tags = var.tags
}

resource "azurerm_private_endpoint" "key_vault" {
  for_each = local.vaults

  name                = "pe-${each.value.name}"
  location            = var.location
  resource_group_name = each.value.resource_group
  subnet_id           = var.private_endpoints_subnet_id

  private_service_connection {
    name                           = "vault"
    private_connection_resource_id = azurerm_key_vault.this[each.key].id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_ids.key_vault]
  }

  tags = var.tags
}

# Every secret read, write and denied request, per vault.
resource "azurerm_monitor_diagnostic_setting" "key_vault" {
  for_each = local.vaults

  name                       = "kv-audit-to-log-workorders"
  target_resource_id         = azurerm_key_vault.this[each.key].id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "AuditEvent"
  }
}

# --- Secrets in <kv-workorders-{env}> (§7.8) --------------------------------------------------------
# id-env-lifecycle-<class> writes them with Key Vault Secrets Officer on rg-workorders-<env>
# (foundation). The platform vault is written by people (docs/bootstrap.md,
# docs/runbooks/credential-rotation.md); this identity holds no data-plane role there.

resource "azurerm_key_vault_secret" "appinsights_connection_string" {
  for_each = toset(local.envs)

  name         = "workorders-appinsights-connection-string"
  key_vault_id = azurerm_key_vault.this[each.key].id
  value        = azurerm_application_insights.env[each.key].connection_string
  content_type = "text/plain"
}

resource "azurerm_key_vault_secret" "sql_migrator_password" {
  for_each = toset(local.envs)

  name             = "workorders-sql-migrator-password"
  key_vault_id     = azurerm_key_vault.this[each.key].id
  value_wo         = ephemeral.random_password.sql_migrator[each.key].result
  value_wo_version = 1
  content_type     = "password; user workorders_migrator; rotated by rotate-sql-passwords"
}

resource "azurerm_key_vault_secret" "sql_acceptance_password" {
  for_each = toset([for e in local.envs : e if e == "tdd"])

  name             = "workorders-sql-acceptance-password"
  key_vault_id     = azurerm_key_vault.this[each.key].id
  value_wo         = ephemeral.random_password.sql_acceptance[each.key].result
  value_wo_version = 1
  content_type     = "password; user workorders_acceptance (tdd only); rotated by rotate-sql-passwords"
}

resource "azurerm_key_vault_secret" "api_validation_key" {
  for_each = toset(local.envs)

  name             = "workorders-api-validation-key"
  key_vault_id     = azurerm_key_vault.this[each.key].id
  value_wo         = ephemeral.random_password.api_validation_key[each.key].result
  value_wo_version = 1
  content_type     = "shared key; ApiKeyAuthentication__ValidationKey"
}

# The Azure OpenAI key is supplied by a person (R15). Terraform creates the secret with an
# obvious non-key value so ExternalSecret workorders-app can sync before the key exists; the
# operator then sets the real value (credential-rotation.md) and Terraform never overwrites it.
resource "azurerm_key_vault_secret" "openai_api_key" {
  for_each = toset(local.envs)

  name             = "workorders-ai-openai-apikey"
  key_vault_id     = azurerm_key_vault.this[each.key].id
  value_wo         = "not-set-see-credential-rotation-runbook"
  value_wo_version = 1
  content_type     = "set by operator; AI_OpenAI_ApiKey"
}
