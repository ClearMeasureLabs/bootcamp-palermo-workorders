# Outputs of one class (§7.10): cluster OIDC issuer, SQL FQDNs and vault URIs. People copy them by
# pull request into gitops/workorders/envs/<env>/config and the Terraform-managed library set
# 'WorkOrders Environment'; the Entra administrator copies oidc_issuer_url into the foundation
# variable aks_oidc_issuer_urls. None is secret.

data "azurerm_user_assigned_identity" "app" {
  for_each = toset(local.envs)

  name                = provider::azurerm::parse_resource_id(var.workload_identity_ids[each.key].app)["resource_name"]
  resource_group_name = provider::azurerm::parse_resource_id(var.workload_identity_ids[each.key].app)["resource_group_name"]
}

output "cluster_name" {
  description = "AKS cluster name."
  value       = azurerm_kubernetes_cluster.this.name
}

output "cluster_id" {
  description = "AKS cluster resource ID."
  value       = azurerm_kubernetes_cluster.this.id
}

output "oidc_issuer_url" {
  description = "Cluster OIDC issuer. Changes on rebuild: update aks_oidc_issuer_urls in the foundation and re-apply."
  value       = azurerm_kubernetes_cluster.this.oidc_issuer_url
}

output "node_resource_group" {
  description = "Node resource group created by AKS (Q17 fallback scope)."
  value       = azurerm_kubernetes_cluster.this.node_resource_group
}

output "sql_server_fqdns" {
  description = "SQL server FQDN per environment (Sql.ServerFqdn)."
  value       = { for e, s in azurerm_mssql_server.env : e => s.fully_qualified_domain_name }
}

output "sql_database_names" {
  description = "Database name per environment (Sql.Database)."
  value       = { for e, d in azurerm_mssql_database.env : e => d.name }
}

output "key_vault_uris" {
  description = "Vault URI per environment, plus key 'platform' for the cluster's platform vault."
  value       = { for k, v in azurerm_key_vault.this : k => v.vault_uri }
}

output "app_insights_ids" {
  description = "App Insights resource ID per environment. The connection string is in Key Vault only."
  value       = { for e, a in azurerm_application_insights.env : e => a.id }
}

output "db_principals" {
  description = "Inputs for configure-db-principals-<env> (env-apply): CREATE USER [<name>] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '<object_id>' for the app identity, plus the password users read from the vault."
  value = {
    for e in local.envs : e => {
      app_identity_name      = data.azurerm_user_assigned_identity.app[e].name
      app_identity_object_id = data.azurerm_user_assigned_identity.app[e].principal_id
      password_users         = e == "tdd" ? ["workorders_migrator", "workorders_acceptance"] : ["workorders_migrator"]
    }
  }
}
