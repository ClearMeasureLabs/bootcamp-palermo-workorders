# Octopus objects that config-as-code does not store (E26): environments, lifecycles, projects and their
# version-control settings, channels, feeds, accounts, worker pools, library variable sets, teams, roles,
# service accounts, the OIDC identity, triggers and the prod freeze (platform-design §7.2, ADR-D7).
# Applied by a platform engineer from a workstation; never from a pipeline (ADR-D8: no Octopus API key in CI).
#
# Objects the user already stored are looked up by name and never created or managed:
#   account `Azure Runtime Provisioner`, library variable sets `Azure Runtime Provisioning` and
#   `GitHub AISF Sample Apps`, Git credential `GitHub clearmeasure-aisf-sample-apps` (§5.3).

terraform {
  # 1.7 or later: import blocks with for_each (channels.tf).
  required_version = ">= 1.7.0"

  required_providers {
    octopusdeploy = {
      source  = "OctopusDeploy/octopusdeploy"
      version = "1.20.0"
    }
  }

  # Partial azurerm backend in the shared state account (§7.10), completed at init time:
  #   terraform init \
  #     -backend-config=resource_group_name=rg-workorders-shared \
  #     -backend-config=storage_account_name=<tfstate-storage-account> \
  #     -backend-config=container_name=tfstate \
  #     -backend-config=key=<octopus-tfstate-key> \
  #     -backend-config=use_azuread_auth=true
  # [CONTRACT GAP] §7.10 names state keys for the two Azure layers only; the key for this layer is undecided.
  # No secret is managed here: OIDC accounts and the feed carry no credentials and no sensitive variable is set.
  # The stored-account lookup reads account metadata only; Octopus returns sensitive fields as "has value" flags
  # [VERIFY that the data source keeps no secret material in state].
  backend "azurerm" {}
}
