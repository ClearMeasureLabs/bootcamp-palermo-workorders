# Environments, in order (§7.2). The names are also the slugs that OCL, accounts and Argo CD annotations use
# (argo.octopus.com/environment: tdd|uat|prod, §7.3).

locals {
  environments = {
    "tdd"           = { sort_order = 0, description = "Work Orders TDD on aks-workorders-nonprod. Automatic from phase 2; destructive acceptance tests run here only (ADR-C11)." }
    "uat"           = { sort_order = 1, description = "Work Orders UAT on aks-workorders-nonprod. Manual promotion and UAT sign-off." }
    "prod"          = { sort_order = 2, description = "Work Orders production on aks-workorders-prod. Prod go/no-go, separation-of-duties guard, weekend freeze." }
    "infra-nonprod" = { sort_order = 3, description = "Runbook-only environment for terraform/environment class nonprod (workorders-infrastructure)." }
    "infra-prod"    = { sort_order = 4, description = "Runbook-only environment for terraform/environment class prod (workorders-infrastructure). OIDC only; no destroy." }
  }

  app_environments   = ["tdd", "uat", "prod"]
  infra_environments = { "infra-nonprod" = "nonprod", "infra-prod" = "prod" }
}

resource "octopusdeploy_environment" "this" {
  for_each = local.environments

  name                         = each.key
  slug                         = each.key
  description                  = each.value.description
  sort_order                   = each.value.sort_order
  allow_dynamic_infrastructure = false
  use_guided_failure           = false
}
