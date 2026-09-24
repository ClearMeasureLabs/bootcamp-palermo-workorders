# AKS cluster aks-workorders-<class> (ADR-D1, §7.10): Standard SKU with explicit controls, not
# AKS Automatic.
#   - Pre-created identities from the foundation: id-aks-<class>-controlplane (Network
#     Contributor on the node subnet, Managed Identity Operator on the kubelet identity) and
#     id-aks-<class>-kubelet (AcrPull). AKS therefore needs no role assignment at creation (Q17).
#   - Workload identity and OIDC issuer on; Entra integration with Azure RBAC; local accounts off.
#   - Node pools `system` (critical add-ons) and `apps`.
#   - No node auto-provisioning, so the nonprod cluster can be stopped (E42).
#   - Run command off: `az aks command invoke` would bypass the kubelogin/Azure RBAC path.
#   - Azure CNI overlay with Cilium enforces the NetworkPolicies of ADR-D12.
# The node resource group keeps its default name MC_<rg>_<cluster>_<region> (Q17 fallback grant).

data "azurerm_user_assigned_identity" "kubelet" {
  name                = provider::azurerm::parse_resource_id(var.aks_kubelet_identity_id)["resource_name"]
  resource_group_name = provider::azurerm::parse_resource_id(var.aks_kubelet_identity_id)["resource_group_name"]
}

resource "azurerm_kubernetes_cluster" "this" {
  name                = local.cluster_name
  location            = var.location
  resource_group_name = local.rg_aks
  dns_prefix          = local.cluster_name
  sku_tier            = local.aks_sku_tier
  kubernetes_version  = var.kubernetes_version

  automatic_upgrade_channel = "patch"
  node_os_upgrade_channel   = "NodeImage"

  oidc_issuer_enabled               = true
  workload_identity_enabled         = true
  local_account_disabled            = true
  role_based_access_control_enabled = true
  run_command_enabled               = false
  image_cleaner_enabled             = true
  image_cleaner_interval_hours      = 48

  # azurerm 5.x requires the block; Manual = no node auto-provisioning.
  node_provisioning_profile {
    mode = "Manual"
  }

  azure_active_directory_role_based_access_control {
    tenant_id          = var.tenant_id
    azure_rbac_enabled = true
    # No Kubernetes-level admin group: cluster rights come only from Azure role assignments in
    # the foundation (standing for id-env-lifecycle-<class>, PIM-eligible for break-glass).
    admin_group_object_ids = []
  }

  dynamic "api_server_access_profile" {
    for_each = length(var.api_server_authorized_ip_ranges) > 0 ? [1] : []

    content {
      authorized_ip_ranges = var.api_server_authorized_ip_ranges
    }
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [var.aks_controlplane_identity_id]
  }

  kubelet_identity {
    client_id                 = data.azurerm_user_assigned_identity.kubelet.client_id
    object_id                 = data.azurerm_user_assigned_identity.kubelet.principal_id
    user_assigned_identity_id = var.aks_kubelet_identity_id
  }

  default_node_pool {
    name                         = "system"
    vm_size                      = var.system_node_pool.vm_size
    vnet_subnet_id               = var.aks_nodes_subnet_id
    auto_scaling_enabled         = true
    min_count                    = var.system_node_pool.min_count
    max_count                    = var.system_node_pool.max_count
    only_critical_addons_enabled = true
    os_sku                       = "AzureLinux"
    temporary_name_for_rotation  = "systemtmp"

    upgrade_settings {
      max_surge = "33%"
    }
  }

  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = "overlay"
    network_data_plane  = "cilium"
    network_policy      = "cilium"
    pod_cidr            = var.cluster_network.pod_cidr
    service_cidr        = var.cluster_network.service_cidr
    dns_service_ip      = var.cluster_network.dns_service_ip
    outbound_type       = "loadBalancer"
    load_balancer_sku   = "standard"
  }

  tags = var.tags

  lifecycle {
    # The autoscaler owns node counts; the patch channel owns patch versions.
    ignore_changes = [
      default_node_pool[0].node_count,
      kubernetes_version,
    ]
  }
}

resource "azurerm_kubernetes_cluster_node_pool" "apps" {
  name                  = "apps"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.this.id
  mode                  = "User"
  vm_size               = var.apps_node_pool.vm_size
  vnet_subnet_id        = var.aks_nodes_subnet_id
  auto_scaling_enabled  = true
  min_count             = var.apps_node_pool.min_count
  max_count             = var.apps_node_pool.max_count
  os_sku                = "AzureLinux"

  upgrade_settings {
    max_surge = "33%"
  }

  tags = var.tags

  lifecycle {
    ignore_changes = [node_count]
  }
}

# Control-plane audit to log-workorders: who changed what in the cluster, including every
# break-glass action (docs/runbooks/break-glass.md, audit evidence).
resource "azurerm_monitor_diagnostic_setting" "aks" {
  name                       = "aks-audit-to-log-workorders"
  target_resource_id         = azurerm_kubernetes_cluster.this.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  # kube-audit-admin excludes get/list events, which keeps ingestion affordable.
  enabled_log {
    category = "kube-audit-admin"
  }

  enabled_log {
    category = "guard"
  }
}
