# The six resource groups of §7.1. The foundation owns the groups; the environment layer only
# creates resources inside them, and env-destroy never deletes a group (ADR-D10).
#
#   rg-workorders-shared        ACR, Log Analytics, Terraform state, Octopus-facing identities
#   rg-workorders-aks-<class>   VNet, private DNS zones, cluster identities, cluster, platform vault
#   rg-workorders-<env>         SQL, Key Vault, App Insights and the workload identities of <env>

resource "azurerm_resource_group" "this" {
  for_each = toset(local.resource_group_names)

  name     = each.key
  location = var.location
  tags     = var.tags
}
