# terraform/environment: one cluster class per state (ADR-D10, design §7.10).
#
# Applied only by the Octopus project workorders-infrastructure (runbooks env-plan, env-apply,
# env-destroy) with account #{Azure.LifecycleAccount}: id-env-lifecycle-<class> through OIDC, or
# the stored 'Azure Runtime Provisioner' in infra-nonprod during phases 1-2 (ADR-C10).
# That identity holds Contributor on the class's resource groups plus the data-plane roles the
# foundation grants; it cannot create role assignments, locks or policy assignments (E36), so
# this layer contains none. The boundary lint (TB08) enforces it.
#
# State: environment-<class>.tfstate in container tfstate of <tfstate-storage-account>. The
# runbooks pass the backend settings from library variable set 'WorkOrders Infrastructure':
#   -backend-config="resource_group_name=#{Terraform.StateResourceGroup}"
#   -backend-config="storage_account_name=#{Terraform.StateStorageAccount}"
#   -backend-config="container_name=#{Terraform.StateContainer}"
#   -backend-config="key=#{Terraform.StateKey}"
#   -backend-config="use_azuread_auth=true"
# Octopus variable substitution in *.tf files stays off (E29); only the backend settings and
# -var-file carry Octopus values.

terraform {
  # 1.11+: write-only arguments (value_wo, data_wo, set_wo, administrator_login_password_wo) and
  # ephemeral resources keep generated passwords and bootstrap tokens out of state.
  required_version = ">= 1.11.0"

  required_providers {
    # Current majors, checked on the registry 2026-09-24 (see terraform/foundation/versions.tf).
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.6"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 3.3"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 3.2"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
  }

  backend "azurerm" {}
}
