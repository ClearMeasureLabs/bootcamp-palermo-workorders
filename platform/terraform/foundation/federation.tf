# Octopus-issuer federated credentials (§5.2, §5.4, ADR-D8). Created once, in advance, by the
# Owner; nothing federated is created by hand. The workload credentials with cluster issuers are
# created by the environment layer after each cluster exists (terraform/environment/
# workload-federation.tf).
#
# Issuer: <OCTOPUS_URL> with no trailing slash (E21). Audience: api://AzureADTokenExchange, the
# audience of the Octopus Azure OIDC accounts (§7.2).
#
# Subjects use the default Octopus execution subject keys space, project and environment (E20),
# which octopus/terraform sets on every account:
#   space:<space-slug>:project:<project-slug>:environment:<environment-slug>
# The feed subject uses the keys space and feed (E30) [VERIFY exact format in the phase-1
# spike: copy the subject from a failed token exchange if it differs].
#
# Each identity carries one Octopus credential, so no identity receives concurrent writes
# (concurrent writes to one identity return 409). Limit: 20 credentials per identity.

locals {
  octopus_federations = merge(
    {
      for e in local.envs : "octopus-deploy-${e}" => {
        identity_id = azurerm_user_assigned_identity.octopus_deploy[e].id
        subject     = "space:${var.octopus_space_slug}:project:workorders:environment:${e}"
      }
    },
    {
      for c in local.classes : "octopus-env-lifecycle-${c}" => {
        identity_id = azurerm_user_assigned_identity.env_lifecycle[c].id
        subject     = "space:${var.octopus_space_slug}:project:workorders-infrastructure:environment:infra-${c}"
      }
    },
    {
      "octopus-feed-acr-workorders" = {
        identity_id = azurerm_user_assigned_identity.octopus_acr_pull.id
        subject     = "space:${var.octopus_space_slug}:feed:acr-workorders"
      }
    },
  )
}

resource "azurerm_federated_identity_credential" "octopus" {
  for_each = local.octopus_federations

  name                      = each.key
  user_assigned_identity_id = each.value.identity_id
  audience                  = [local.federation_audience]
  issuer                    = local.octopus_issuer
  subject                   = each.value.subject
}
