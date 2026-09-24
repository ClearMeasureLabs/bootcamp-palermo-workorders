# Locks and Azure Policy assignments. Only an Owner or User Access Administrator can create or
# delete either (E36), so no runbook identity can remove them.

# --- CanNotDelete locks (§7.10, ADR-C10 recommendation 5, R6) ----------------------------------
# The locks also block deletes of child resources: deleting an AKS node pool, a federated
# credential in rg-workorders-prod, or an expired pre-release database copy needs the lock lifted
# for the change window (docs/runbooks/break-glass.md, "Lift a lock").
locals {
  locks = merge(
    {
      "rg-workorders-prod"     = azurerm_resource_group.this["rg-workorders-prod"].id
      "rg-workorders-aks-prod" = azurerm_resource_group.this["rg-workorders-aks-prod"].id
      # Losing state would orphan every managed resource.
      "tfstate-account" = azurerm_storage_account.tfstate.id
    },
    # Legacy Container Apps resource groups, by ID from the untracked tfvars.
    { for idx, id in var.legacy_resource_group_ids : "legacy-${idx}" => id },
  )
}

resource "azurerm_management_lock" "cannot_delete" {
  for_each = local.locks

  name       = "workorders-cannot-delete"
  scope      = each.value
  lock_level = "CanNotDelete"
  notes      = "workorders platform (terraform/foundation). Lift only through docs/runbooks/break-glass.md."
}

# --- Azure Policy (§7.10) -------------------------------------------------------------------------
# Assigned per workorders resource group, not at subscription scope, so other workloads in the
# subscription are unaffected. Built-in definition IDs checked 2026-09-24 on
# https://www.azadvertizer.net (source: Azure/azure-policy built-ins).
locals {
  policy = {
    # "Azure Key Vault should use RBAC permission model" (effects Audit, Deny, Disabled). Deny:
    # under access policies a Contributor can grant itself data-plane access (ADR-D9).
    kv_rbac = "/providers/Microsoft.Authorization/policyDefinitions/12d4fa5e-1f9f-4c21-97a9-b99b3c6611b5"
    # "Azure SQL Database should have Microsoft Entra-only authentication enabled" (effects Audit,
    # Deny, Disabled). Audit only: SQL authentication stays on until WI-05 (ADR-D9).
    sql_entra_only = "/providers/Microsoft.Authorization/policyDefinitions/b3a22bc9-66de-45fb-98fa-00f5df42f41a"
    # "[Preview]: Managed Identity Federated Credentials should be from allowed issuer types".
    fic_issuers = "/providers/Microsoft.Authorization/policyDefinitions/2571b7c3-3056-4a61-b00a-9bc5232234f5"
  }

  env_resource_group_names = [for e in local.envs : local.rg_env[e]]

  # Before the clusters exist any AKS issuer is allowed (allowAKS). Once aks_oidc_issuer_urls is
  # filled, only the two exact cluster issuers and Octopus remain allowed, so an identity with
  # write access to a workload identity cannot federate it to a foreign cluster.
  pin_aks_issuers = length(var.aks_oidc_issuer_urls) == length(local.classes)
}

resource "azurerm_resource_group_policy_assignment" "kv_rbac" {
  for_each = toset(local.resource_group_names)

  name                 = "workorders-kv-rbac-model"
  display_name         = "workorders: Key Vault must use the RBAC permission model"
  resource_group_id    = azurerm_resource_group.this[each.key].id
  policy_definition_id = local.policy.kv_rbac
  parameters           = jsonencode({ effect = { value = "Deny" } })

  non_compliance_message {
    content = "Key Vaults in the workorders platform must set rbac_authorization_enabled = true (ADR-D9)."
  }
}

resource "azurerm_resource_group_policy_assignment" "sql_entra_only" {
  for_each = toset(local.env_resource_group_names)

  name                 = "workorders-sql-entra-only-audit"
  display_name         = "workorders: audit SQL servers without Entra-only authentication (until WI-05)"
  resource_group_id    = azurerm_resource_group.this[each.key].id
  policy_definition_id = local.policy.sql_entra_only
  parameters           = jsonencode({ effect = { value = "Audit" } })
}

resource "azurerm_resource_group_policy_assignment" "fic_issuers" {
  for_each = toset(local.resource_group_names)

  name                 = "workorders-fic-allowed-issuers"
  display_name         = "workorders: federated credentials only from Octopus and the workorders clusters"
  resource_group_id    = azurerm_resource_group.this[each.key].id
  policy_definition_id = local.policy.fic_issuers
  parameters = jsonencode({
    allowFederatedCredentials = { value = true }
    allowAKS                  = { value = !local.pin_aks_issuers }
    allowGitHub               = { value = false }
    allowAWS                  = { value = false }
    allowGCS                  = { value = false }
    allowedIssuerExceptions = {
      value = concat([local.octopus_issuer], local.pin_aks_issuers ? values(var.aks_oidc_issuer_urls) : [])
    }
    effect = { value = var.fic_issuer_policy_effect }
  })

  non_compliance_message {
    content = "Federated credentials in the workorders platform may trust only ${local.octopus_issuer} and the workorders AKS OIDC issuers (ADR-D8)."
  }
}
