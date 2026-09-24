provider "azurerm" {
  subscription_id = var.subscription_id
  tenant_id       = var.tenant_id

  # The foundation (Owner) registered every resource provider. This identity holds Contributor
  # on resource groups only and cannot register providers, so the 5.x default "none" stays.
  resource_provider_registrations = "none"

  # The state account and the Key Vaults are reached with Entra ID only.
  storage_use_azuread = true

  features {
    key_vault {
      # Purge protection is on; env-destroy soft-deletes and env-apply recovers.
      purge_soft_delete_on_destroy          = false
      recover_soft_deleted_key_vaults       = true
      purge_soft_deleted_secrets_on_destroy = false
      recover_soft_deleted_secrets          = true
    }
  }
}

# --- Kubernetes and Helm: Entra ID through kubelogin, never a local account ---------------------
# Local accounts are disabled on both clusters (aks.tf), so there is no client certificate or
# static token. The providers call kubelogin as an exec credential plugin; it exchanges the
# runbook's Azure identity for an Entra token for the AKS server application
# 6dae42f8-4368-4678-94ff-3960e28e3630 (the Azure Kubernetes Service AAD Server, the same for
# every AKS cluster). The identity needs Azure Kubernetes Service RBAC Cluster Admin on
# rg-workorders-aks-<class> (foundation).
#
# kubelogin_login_mode maps to the Octopus account type (§7.2 Azure.LifecycleAccount):
#   azurecli          the step logged the Azure CLI in with the account first [VERIFY whether the
#                     built-in Terraform step does this for Azure accounts]
#   spn               client-secret account; kubelogin reads AAD_SERVICE_PRINCIPAL_CLIENT_ID and
#                     AAD_SERVICE_PRINCIPAL_CLIENT_SECRET from the step environment
#   workloadidentity  OIDC account; kubelogin reads AZURE_CLIENT_ID, AZURE_TENANT_ID and
#                     AZURE_FEDERATED_TOKEN_FILE [VERIFY how the step exposes the Octopus token]
# The worker-tools image must contain kubelogin (`az aks install-cli` installs it) [VERIFY].
#
# The providers read the cluster endpoint from azurerm_kubernetes_cluster.this, so the very first
# apply of a class may need two passes: -target=azurerm_kubernetes_cluster.this, then a full
# apply [VERIFY in the phase-2 spike].

locals {
  kube_host = azurerm_kubernetes_cluster.this.kube_config[0].host
  kube_ca   = base64decode(azurerm_kubernetes_cluster.this.kube_config[0].cluster_ca_certificate)

  kubelogin_args = [
    "get-token",
    "--login", var.kubelogin_login_mode,
    "--server-id", "6dae42f8-4368-4678-94ff-3960e28e3630",
    "--tenant-id", var.tenant_id,
  ]
}

provider "kubernetes" {
  host                   = local.kube_host
  cluster_ca_certificate = local.kube_ca

  exec {
    api_version = "client.authentication.k8s.io/v1beta1"
    command     = "kubelogin"
    args        = local.kubelogin_args
  }
}

provider "helm" {
  kubernetes = {
    host                   = local.kube_host
    cluster_ca_certificate = local.kube_ca

    exec = {
      api_version = "client.authentication.k8s.io/v1beta1"
      command     = "kubelogin"
      args        = local.kubelogin_args
    }
  }
}
