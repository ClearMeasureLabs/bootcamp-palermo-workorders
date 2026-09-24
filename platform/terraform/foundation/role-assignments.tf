# Every Azure role assignment of the platform (§5.2, §5.4, ADR-D10). terraform/environment
# holds none: Contributor cannot write Microsoft.Authorization/* (E36), and the boundary lint
# (scripts/checks/tool-boundaries.sh, TB08) fails the build if one appears there.
#
# Rules applied here:
#   - A grant that must reach a resource created later (Key Vault, SQL server, AKS) is assigned
#     at resource-group scope in advance (§5.4).
#   - Grants go directly to managed identities, never through groups, because group membership
#     for managed identities can lag by up to 24 hours. The one exception is the SQL Entra admin
#     group (entra.tf).
#   - principal_type is set for managed identities so new principals do not fail on Entra
#     replication delay.

data "azurerm_subscription" "current" {}

locals {
  class_envs = { for c in local.classes : c => [for e in local.envs : e if local.env_class[e] == c] }

  # Blob paths each provisioning identity may touch in the shared tfstate container. The
  # conditions keep the nonprod identities away from prod and foundation state.
  # [VERIFY] the azurerm backend lists no blobs during init/plan/apply; if the phase-2 spike
  # proves otherwise, set state_path_conditions_enabled = false.
  state_blob_condition = {
    for c in local.classes : c => <<-EOT
      (
       (
        !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read'})
        AND
        !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/write'})
        AND
        !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/add/action'})
        AND
        !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/delete'})
        AND
        !(ActionMatches{'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/move/action'})
       )
       OR
       (
        @Resource[Microsoft.Storage/storageAccounts/blobServices/containers/blobs:path] StringStartsWith 'environment-${c}.tfstate'
       )
      )
    EOT
  }

  # --- Octopus deployment identities: id-octopus-deploy-<env> -------------------------------
  deploy_grants = merge(
    {
      for e in local.envs : "deploy-${e}-kv-secrets-user" => {
        scope     = azurerm_resource_group.this[local.rg_env[e]].id
        role      = "Key Vault Secrets User"
        principal = azurerm_user_assigned_identity.octopus_deploy[e].principal_id
      }
    },
    {
      for e in local.envs : "deploy-${e}-reader" => {
        scope     = azurerm_resource_group.this[local.rg_env[e]].id
        role      = "Reader"
        principal = azurerm_user_assigned_identity.octopus_deploy[e].principal_id
      }
    },
    {
      # db-copy-pre-release and db-restore-pitr (prod); see the variable for uat.
      for e in var.deploy_identity_sql_db_contributor_envs : "deploy-${e}-sql-db-contributor" => {
        scope     = azurerm_resource_group.this[local.rg_env[e]].id
        role      = "SQL DB Contributor"
        principal = azurerm_user_assigned_identity.octopus_deploy[e].principal_id
      }
    },
  )

  # --- Environment lifecycle: id-env-lifecycle-<class> ---------------------------------------
  lifecycle_grants = merge(
    {
      for c in local.classes : "lifecycle-${c}-contributor-aks" => {
        scope     = azurerm_resource_group.this[local.rg_aks[c]].id
        role      = "Contributor"
        principal = azurerm_user_assigned_identity.env_lifecycle[c].principal_id
      }
    },
    {
      for pair in flatten([for c in local.classes : [for e in local.class_envs[c] : { c = c, e = e }]]) :
      "lifecycle-${pair.c}-contributor-${pair.e}" => {
        scope     = azurerm_resource_group.this[local.rg_env[pair.e]].id
        role      = "Contributor"
        principal = azurerm_user_assigned_identity.env_lifecycle[pair.c].principal_id
      }
    },
    {
      # Helm and Kubernetes providers in bootstrap.tf (Argo CD, Octopus workers).
      for c in local.classes : "lifecycle-${c}-aks-rbac-cluster-admin" => {
        scope     = azurerm_resource_group.this[local.rg_aks[c]].id
        role      = "Azure Kubernetes Service RBAC Cluster Admin"
        principal = azurerm_user_assigned_identity.env_lifecycle[c].principal_id
      }
    },
    {
      # Contributor alone cannot write secrets into an RBAC vault (ADR-D9).
      for pair in flatten([for c in local.classes : [for e in local.class_envs[c] : { c = c, e = e }]]) :
      "lifecycle-${pair.c}-kv-secrets-officer-${pair.e}" => {
        scope     = azurerm_resource_group.this[local.rg_env[pair.e]].id
        role      = "Key Vault Secrets Officer"
        principal = azurerm_user_assigned_identity.env_lifecycle[pair.c].principal_id
      }
    },
    {
      for c in local.classes : "lifecycle-${c}-state-blob-contributor" => {
        scope     = azurerm_storage_container.tfstate.id
        role      = "Storage Blob Data Contributor"
        principal = azurerm_user_assigned_identity.env_lifecycle[c].principal_id
        condition = var.state_path_conditions_enabled ? local.state_blob_condition[c] : null
      }
    },
    {
      # Q17 fallback, off by default.
      for c in(var.lifecycle_node_rg_contributor_enabled ? local.classes : []) : "lifecycle-${c}-contributor-node-rg" => {
        scope     = "${data.azurerm_subscription.current.id}/resourceGroups/MC_${local.rg_aks[c]}_${local.cluster_name[c]}_${var.location}"
        role      = "Contributor"
        principal = azurerm_user_assigned_identity.env_lifecycle[c].principal_id
      }
    },
    {
      for c in(var.lifecycle_workspace_reader_enabled ? local.classes : []) : "lifecycle-${c}-workspace-reader" => {
        scope     = azurerm_log_analytics_workspace.this.id
        role      = "Reader"
        principal = azurerm_user_assigned_identity.env_lifecycle[c].principal_id
      }
    },
  )

  # --- Stored 'Azure Runtime Provisioner' (interim, infra-nonprod only, ADR-C10) -------------
  # Its Contributor role at subscription scope was granted outside this repository by the user
  # and is not managed here. These are the extra grants it needs to run the nonprod
  # environment layer in phases 1-2; provisioner_grants_enabled = false removes them at the
  # phase-2 exit (R4).
  provisioner_active = var.provisioner_object_id != null && var.provisioner_grants_enabled
  provisioner_grants = {
    for key, grant in merge(
      {
        "provisioner-aks-rbac-cluster-admin-nonprod" = {
          scope     = azurerm_resource_group.this[local.rg_aks["nonprod"]].id
          role      = "Azure Kubernetes Service RBAC Cluster Admin"
          principal = var.provisioner_object_id
        }
        "provisioner-state-blob-contributor" = {
          scope     = azurerm_storage_container.tfstate.id
          role      = "Storage Blob Data Contributor"
          principal = var.provisioner_object_id
          condition = var.state_path_conditions_enabled ? local.state_blob_condition["nonprod"] : null
        }
      },
      {
        for e in local.class_envs["nonprod"] : "provisioner-kv-secrets-officer-${e}" => {
          scope     = azurerm_resource_group.this[local.rg_env[e]].id
          role      = "Key Vault Secrets Officer"
          principal = var.provisioner_object_id
        }
      },
  ) : key => grant if local.provisioner_active }

  # --- AKS control plane and kubelet ---------------------------------------------------------
  cluster_grants = merge(
    {
      for c in local.classes : "aks-${c}-controlplane-network-contributor" => {
        scope     = azurerm_subnet.aks_nodes[c].id
        role      = "Network Contributor"
        principal = azurerm_user_assigned_identity.aks_controlplane[c].principal_id
      }
    },
    {
      for c in local.classes : "aks-${c}-controlplane-mi-operator" => {
        scope     = azurerm_user_assigned_identity.aks_kubelet[c].id
        role      = "Managed Identity Operator"
        principal = azurerm_user_assigned_identity.aks_controlplane[c].principal_id
      }
    },
    {
      for c in local.classes : "aks-${c}-kubelet-acrpull" => {
        scope     = azurerm_container_registry.this.id
        role      = "AcrPull"
        principal = azurerm_user_assigned_identity.aks_kubelet[c].principal_id
      }
    },
  )

  # --- Workload identities (§7.8) --------------------------------------------------------------
  workload_grants = merge(
    {
      for e in local.envs : "eso-${e}-kv-secrets-user" => {
        scope     = azurerm_resource_group.this[local.rg_env[e]].id
        role      = "Key Vault Secrets User"
        principal = azurerm_user_assigned_identity.workorders_eso[e].principal_id
      }
    },
    {
      # <kv-workorders-platform-<cluster>> is created later in rg-workorders-aks-<class>, the only
      # vault in that group.
      for c in local.classes : "eso-platform-${c}-kv-secrets-user" => {
        scope     = azurerm_resource_group.this[local.rg_aks[c]].id
        role      = "Key Vault Secrets User"
        principal = azurerm_user_assigned_identity.eso_platform[c].principal_id
      }
    },
    {
      # Kyverno reads signatures and attestations from ACR.
      for c in local.classes : "kyverno-${c}-acrpull" => {
        scope     = azurerm_container_registry.this.id
        role      = "AcrPull"
        principal = azurerm_user_assigned_identity.kyverno[c].principal_id
      }
    },
    {
      "octopus-feed-acrpull" = {
        scope     = azurerm_container_registry.this.id
        role      = "AcrPull"
        principal = azurerm_user_assigned_identity.octopus_acr_pull.principal_id
      }
    },
  )

  # --- People who run this layer ---------------------------------------------------------------
  operator_grants = {
    for oid in var.state_operator_object_ids : "state-operator-${oid}" => {
      scope     = azurerm_storage_container.tfstate.id
      role      = "Storage Blob Data Contributor"
      principal = oid
      type      = null
    }
  }

  # Defaults: managed-identity principal, no condition.
  role_assignments = {
    for key, grant in merge(
      local.deploy_grants,
      local.lifecycle_grants,
      local.provisioner_grants,
      local.cluster_grants,
      local.workload_grants,
      local.operator_grants,
    ) : key => merge({ type = "ServicePrincipal", condition = null }, grant)
  }
}

resource "azurerm_role_assignment" "this" {
  for_each = local.role_assignments

  scope                = each.value.scope
  role_definition_name = each.value.role
  principal_id         = each.value.principal
  principal_type       = each.value.type
  condition            = each.value.condition
  condition_version    = each.value.condition == null ? null : "2.0"
  description          = "workorders platform: ${each.key} (terraform/foundation)"
}

# --- Break-glass: eligible, never standing (docs/runbooks/break-glass.md) ----------------------
# Team SRE On-call activates these through PIM with a justification and ticket; activation is
# time-bound by the PIM role settings. Permanent eligibility requires the PIM policy to allow it
# [VERIFY]; otherwise add a schedule block with an expiration.

data "azurerm_role_definition" "breakglass" {
  for_each = toset(["Azure Kubernetes Service RBAC Cluster Admin", "Key Vault Secrets Officer"])

  name = each.key
}

locals {
  breakglass_grants = var.breakglass_group_object_id == null ? {} : merge(
    {
      for c in local.classes : "breakglass-aks-rbac-cluster-admin-${c}" => {
        scope = azurerm_resource_group.this[local.rg_aks[c]].id
        role  = "Azure Kubernetes Service RBAC Cluster Admin"
      }
    },
    {
      for c in local.classes : "breakglass-kv-secrets-officer-aks-${c}" => {
        scope = azurerm_resource_group.this[local.rg_aks[c]].id
        role  = "Key Vault Secrets Officer"
      }
    },
    {
      for e in local.envs : "breakglass-kv-secrets-officer-${e}" => {
        scope = azurerm_resource_group.this[local.rg_env[e]].id
        role  = "Key Vault Secrets Officer"
      }
    },
  )
}

resource "azurerm_pim_eligible_role_assignment" "breakglass" {
  for_each = local.breakglass_grants

  scope              = each.value.scope
  role_definition_id = "${data.azurerm_subscription.current.id}${data.azurerm_role_definition.breakglass[each.value.role].id}"
  principal_id       = var.breakglass_group_object_id
  justification      = "workorders break-glass and credential rotation (docs/runbooks/break-glass.md)"
}
