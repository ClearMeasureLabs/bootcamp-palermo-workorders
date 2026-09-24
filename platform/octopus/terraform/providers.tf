# Provider authentication: an OIDC access token or an API key, never both (the provider prefers the API key when
# both are set). Pass them as TF_VAR_octopus_access_token or TF_VAR_octopus_api_key in the shell; never in files.
#
# Required Octopus permissions [from https://octopus.com/docs/security/users-and-teams/default-permissions]:
# - Space Manager in <octopus-space> covers environments, lifecycles, projects, channels, feeds, accounts, worker
#   pools, library variable sets, teams, triggers and project freezes.
# - Service accounts (UserEdit) and the custom role `CI Release Publisher` (UserRoleEdit) are System Manager
#   permissions. §5.2 gives the platform engineer Space Manager only [CONTRACT GAP]: a System Manager applies
#   teams.tf, or creates those two objects once for import.

provider "octopusdeploy" {
  address      = var.octopus_url
  space_id     = var.octopus_space_id
  access_token = var.octopus_access_token
  api_key      = var.octopus_api_key
}
