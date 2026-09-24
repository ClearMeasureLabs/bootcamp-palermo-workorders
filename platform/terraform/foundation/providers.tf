provider "azurerm" {
  subscription_id = var.subscription_id
  tenant_id       = var.tenant_id

  # The state account disables shared keys, so data-plane calls use Entra ID.
  storage_use_azuread = true

  # azurerm 5.x registers no resource providers by default. The foundation runs as Owner and
  # registers every provider both layers need, so the environment layer (Contributor on
  # resource groups only, which cannot register providers) never has to.
  resource_providers_to_register = [
    "Microsoft.Authorization",
    "Microsoft.ContainerRegistry",
    "Microsoft.ContainerService",
    "Microsoft.Insights",
    "Microsoft.KeyVault",
    "Microsoft.ManagedIdentity",
    "Microsoft.Network",
    "Microsoft.OperationalInsights",
    "Microsoft.OperationsManagement",
    "Microsoft.PolicyInsights",
    "Microsoft.Sql",
    "Microsoft.Storage",
  ]

  features {
    resource_group {
      # Resource groups hold resources owned by the environment layer; never delete a
      # non-empty group from here.
      prevent_deletion_if_contains_resources = true
    }
  }
}

provider "azuread" {
  tenant_id = var.tenant_id
}
