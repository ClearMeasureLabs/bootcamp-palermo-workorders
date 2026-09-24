# Azure accounts (§5.2, §7.2, ADR-D8, ADR-C10).
#
# New: five Azure OIDC accounts, one per environment or class, each restricted to its environment(s).
# Execution subject keys space, project, environment give
#   space:<space-slug>:project:workorders:environment:<env>                              (deploy accounts)
#   space:<space-slug>:project:workorders-infrastructure:environment:infra-<class>       (lifecycle accounts)
# (E20; https://octopus.com/docs/infrastructure/accounts/openid-connect). terraform/foundation creates the matching
# federated credentials with issuer <OCTOPUS_URL> (no trailing slash) and audience api://AzureADTokenExchange.
# Health-check and account-test subjects keep their defaults; no federated credential exists for them, so the
# "Save and test" button fails by design.
# Account names are also their slugs (OCL references them in Azure.DeployAccount and Azure.LifecycleAccount).
#
# Stored: `Azure Runtime Provisioner` (slug azure-runtime-provisioner) is looked up, never managed. Only
# Azure.LifecycleAccount in infra-nonprod references it, during phases 1-2 (ADR-C10).

resource "octopusdeploy_azure_openid_connect" "deploy" {
  for_each = toset(local.app_environments)

  name                              = "azure-oidc-deploy-${each.key}"
  description                       = "UAMI id-octopus-deploy-${each.key}: Key Vault Secrets User and Reader on rg-workorders-${each.key}${each.key == "prod" ? ", SQL DB Contributor on rg-workorders-prod" : ""}. Project workorders only."
  application_id                    = var.deploy_identity_client_ids[each.key]
  tenant_id                         = var.azure_tenant_id
  subscription_id                   = var.azure_subscription_id
  audience                          = "api://AzureADTokenExchange"
  execution_subject_keys            = ["space", "project", "environment"]
  environments                      = [octopusdeploy_environment.this[each.key].id]
  tenanted_deployment_participation = "Untenanted"
}

resource "octopusdeploy_azure_openid_connect" "env_lifecycle" {
  for_each = { for environment, class in local.infra_environments : class => environment }

  name                              = "azure-oidc-env-lifecycle-${each.key}"
  description                       = "UAMI id-env-lifecycle-${each.key}: sole provisioning identity for terraform/environment class ${each.key}. Project workorders-infrastructure only."
  application_id                    = var.lifecycle_identity_client_ids[each.key]
  tenant_id                         = var.azure_tenant_id
  subscription_id                   = var.azure_subscription_id
  audience                          = "api://AzureADTokenExchange"
  execution_subject_keys            = ["space", "project", "environment"]
  environments                      = [octopusdeploy_environment.this[each.value].id]
  tenanted_deployment_participation = "Untenanted"
}

data "octopusdeploy_accounts" "stored_provisioner" {
  account_type = "AzureServicePrincipal"
  partial_name = var.stored_azure_account_name
  take         = 10

  lifecycle {
    postcondition {
      condition     = length([for a in self.accounts : a if a.name == var.stored_azure_account_name]) == 1
      error_message = "Expected exactly one account named '${var.stored_azure_account_name}'. It is stored by the user and never created here."
    }
  }
}

locals {
  stored_provisioner_account = one([for a in data.octopusdeploy_accounts.stored_provisioner.accounts : a if a.name == var.stored_azure_account_name])
}

# Recommendation R4 / ADR-C10: the stored account should be restricted to infra-nonprod. Terraform does not change
# it; this check reports a warning on every plan until a person restricts it in the Octopus UI.
check "stored_provisioner_restricted_to_infra_nonprod" {
  assert {
    condition     = try(toset(local.stored_provisioner_account.environments) == toset([octopusdeploy_environment.this["infra-nonprod"].id]), false)
    error_message = "Account '${var.stored_azure_account_name}' is not restricted to infra-nonprod only (R4). Restrict it in Octopus; Terraform never manages it."
  }
}
