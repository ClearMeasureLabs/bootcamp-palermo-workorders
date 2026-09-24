# Workload federated credentials (§7.8, §5.4). The issuer is this cluster's OIDC issuer, which
# exists only after the cluster does and changes when the cluster is rebuilt, so this layer owns
# these credentials; the foundation owns the identities and their role assignments. Creating a
# federated credential on a managed identity is an ARM write that Contributor on the identity's
# resource group allows.
#
#   system:serviceaccount:workorders-<env>:ui-server                -> id-workorders-<env>-app
#   system:serviceaccount:workorders-<env>:worker                   -> id-workorders-<env>-app
#   system:serviceaccount:workorders-<env>:workorders-eso           -> id-workorders-<env>-eso
#   system:serviceaccount:external-secrets:external-secrets        -> id-eso-platform-<cluster>
#   system:serviceaccount:kyverno:kyverno-admission-controller     -> id-kyverno-<cluster>
#
# Exact subjects only (no flexible credentials). Concurrent writes to one identity return 409,
# so the second credential on id-workorders-<env>-app waits for the first (depends_on). Limit: 20
# credentials per identity. The foundation's issuer policy allows only the Octopus issuer and,
# once pinned, these two clusters' issuers.

locals {
  oidc_issuer = azurerm_kubernetes_cluster.this.oidc_issuer_url
}

resource "azurerm_federated_identity_credential" "app_ui_server" {
  for_each = toset(local.envs)

  name                      = "${local.cluster_name}-ui-server"
  user_assigned_identity_id = var.workload_identity_ids[each.key].app
  audience                  = [local.federation_audience]
  issuer                    = local.oidc_issuer
  subject                   = "system:serviceaccount:workorders-${each.key}:ui-server"
}

resource "azurerm_federated_identity_credential" "app_worker" {
  for_each = toset(local.envs)

  name                      = "${local.cluster_name}-worker"
  user_assigned_identity_id = var.workload_identity_ids[each.key].app
  audience                  = [local.federation_audience]
  issuer                    = local.oidc_issuer
  subject                   = "system:serviceaccount:workorders-${each.key}:worker"

  # Same identity as app_ui_server: serialize the writes.
  depends_on = [azurerm_federated_identity_credential.app_ui_server]
}

resource "azurerm_federated_identity_credential" "eso" {
  for_each = toset(local.envs)

  name                      = "${local.cluster_name}-workorders-eso"
  user_assigned_identity_id = var.workload_identity_ids[each.key].eso
  audience                  = [local.federation_audience]
  issuer                    = local.oidc_issuer
  subject                   = "system:serviceaccount:workorders-${each.key}:workorders-eso"
}

resource "azurerm_federated_identity_credential" "eso_platform" {
  name                      = "${local.cluster_name}-external-secrets"
  user_assigned_identity_id = var.platform_identity_ids.eso
  audience                  = [local.federation_audience]
  issuer                    = local.oidc_issuer
  subject                   = "system:serviceaccount:external-secrets:external-secrets"
}

resource "azurerm_federated_identity_credential" "kyverno" {
  name                      = "${local.cluster_name}-kyverno-admission-controller"
  user_assigned_identity_id = var.platform_identity_ids.kyverno
  audience                  = [local.federation_audience]
  issuer                    = local.oidc_issuer
  subject                   = "system:serviceaccount:kyverno:kyverno-admission-controller"
}
