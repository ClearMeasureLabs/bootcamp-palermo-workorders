# One VNet per cluster class (§7.10), in the cluster resource group, with:
#   - snet-aks-nodes: AKS nodes. Pods use the Azure CNI overlay (terraform/environment/aks.tf),
#     so pod IPs do not consume this range.
#   - snet-private-endpoints: private endpoints for SQL and Key Vault (environment layer).
#
# Placement is deliberate. id-env-lifecycle-<class> holds Contributor on rg-workorders-aks-<class>,
# which includes the subnets/join and privateDnsZones/join actions the environment layer needs
# to attach private endpoints and register their DNS records. Putting the VNet or the zones in
# rg-workorders-shared would need extra grants there.
#
# id-aks-<class>-controlplane gets Network Contributor on snet-aks-nodes (role-assignments.tf).

resource "azurerm_virtual_network" "class" {
  for_each = var.networks

  name                = "vnet-workorders-${each.key}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this[local.rg_aks[each.key]].name
  address_space       = each.value.address_space
  tags                = var.tags
}

resource "azurerm_subnet" "aks_nodes" {
  for_each = var.networks

  name                 = "snet-aks-nodes"
  resource_group_name  = azurerm_resource_group.this[local.rg_aks[each.key]].name
  virtual_network_name = azurerm_virtual_network.class[each.key].name
  address_prefixes     = [each.value.aks_nodes_prefix]
}

resource "azurerm_subnet" "private_endpoints" {
  for_each = var.networks

  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.this[local.rg_aks[each.key]].name
  virtual_network_name = azurerm_virtual_network.class[each.key].name
  address_prefixes     = [each.value.private_endpoints_prefix]

  # Apply NSG rules to private endpoint traffic as well.
  private_endpoint_network_policies = "Enabled"
}

# Private DNS zones for the two private-link services the environment layer uses. The zone
# names are fixed by Azure; each class has its own copy, linked only to its own VNet.
locals {
  private_dns_zones = {
    sql       = "privatelink.database.windows.net"
    key_vault = "privatelink.vaultcore.azure.net"
  }

  class_zone_pairs = {
    for pair in setproduct(local.classes, keys(local.private_dns_zones)) :
    "${pair[0]}-${pair[1]}" => { class = pair[0], zone = pair[1] }
  }
}

resource "azurerm_private_dns_zone" "this" {
  for_each = local.class_zone_pairs

  name                = local.private_dns_zones[each.value.zone]
  resource_group_name = azurerm_resource_group.this[local.rg_aks[each.value.class]].name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "this" {
  for_each = local.class_zone_pairs

  name                 = "link-vnet-workorders-${each.value.class}"
  private_dns_zone_id  = azurerm_private_dns_zone.this[each.key].id
  virtual_network_id   = azurerm_virtual_network.class[each.value.class].id
  registration_enabled = false
  tags                 = var.tags
}
