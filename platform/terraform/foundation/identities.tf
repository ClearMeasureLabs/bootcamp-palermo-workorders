# Every user-assigned managed identity of §5.2. They are persistent: a cluster rebuild keeps
# them and their role assignments; only the workload federated credentials change (ADR-D10).
#
# Placement decides who can add federated credentials to an identity:
#   rg-workorders-shared       Octopus-facing identities. No environment-layer identity has write
#                              access here, so no runbook can federate them to another issuer.
#   rg-workorders-aks-<class>  cluster identities and platform workload identities; the
#                              environment layer adds their cluster-issuer credentials.
#   rg-workorders-<env>        app workload identities; same reason.

# Octopus deployment accounts azure-oidc-deploy-<env> (§7.2).
resource "azurerm_user_assigned_identity" "octopus_deploy" {
  for_each = toset(local.envs)

  name                = "id-octopus-deploy-${each.key}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  tags                = var.tags
}

# Octopus environment-lifecycle accounts azure-oidc-env-lifecycle-<class> (ADR-C10): the sole
# provisioning identity once the stored provisioner is retired.
resource "azurerm_user_assigned_identity" "env_lifecycle" {
  for_each = toset(local.classes)

  name                = "id-env-lifecycle-${each.key}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  tags                = var.tags
}

# Octopus feed acr-workorders (OIDC, E38).
resource "azurerm_user_assigned_identity" "octopus_acr_pull" {
  name                = "id-octopus-acr-pull"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  tags                = var.tags
}

# AKS control plane and kubelet (pre-created identities, Q17 default).
resource "azurerm_user_assigned_identity" "aks_controlplane" {
  for_each = toset(local.classes)

  name                = "id-aks-${each.key}-controlplane"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_aks[each.key]].name
  tags                = var.tags
}

resource "azurerm_user_assigned_identity" "aks_kubelet" {
  for_each = toset(local.classes)

  name                = "id-aks-${each.key}-kubelet"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_aks[each.key]].name
  tags                = var.tags
}

# Platform workload identities per cluster (§7.8).
resource "azurerm_user_assigned_identity" "eso_platform" {
  for_each = toset(local.classes)

  name                = "id-eso-platform-${each.key}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_aks[each.key]].name
  tags                = var.tags
}

resource "azurerm_user_assigned_identity" "kyverno" {
  for_each = toset(local.classes)

  name                = "id-kyverno-${each.key}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_aks[each.key]].name
  tags                = var.tags
}

# App workload identities per environment (§7.8). id-workorders-<env>-app has no Azure role at
# all: its only right is the contained database user created by configure-db-principals-<env>.
resource "azurerm_user_assigned_identity" "workorders_app" {
  for_each = toset(local.envs)

  name                = "id-workorders-${each.key}-app"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_env[each.key]].name
  tags                = var.tags
}

resource "azurerm_user_assigned_identity" "workorders_eso" {
  for_each = toset(local.envs)

  name                = "id-workorders-${each.key}-eso"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_env[each.key]].name
  tags                = var.tags
}

# Phase 4, after WI-05: replaces the workorders_migrator password (§5.2). Its federated
# credential targets the Kubernetes worker's script-pod service account and waits for Q2.
resource "azurerm_user_assigned_identity" "workorders_migrator" {
  for_each = var.create_migrator_identities ? toset(local.envs) : toset([])

  name                = "id-workorders-${each.key}-migrator"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_env[each.key]].name
  tags                = var.tags
}
