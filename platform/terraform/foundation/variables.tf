# Inputs for the foundation layer. Real values live in an untracked foundation.tfvars copied
# from foundation.tfvars.example. The repository is public: never commit real IDs, names from
# the inventory, IP ranges or e-mail addresses.

variable "tenant_id" {
  description = "Entra tenant ID (<AZURE_TENANT_ID>)."
  type        = string
}

variable "subscription_id" {
  description = "Subscription that holds every workorders resource group (<AZURE_SUBSCRIPTION_ID>)."
  type        = string
}

variable "location" {
  description = "Azure region for every resource (<azure-region>)."
  type        = string
}

variable "octopus_url" {
  description = "Octopus Cloud URL (<OCTOPUS_URL>). It is the OIDC issuer of the Octopus accounts and must have no trailing slash (E21)."
  type        = string

  validation {
    condition     = startswith(var.octopus_url, "https://") && !endswith(var.octopus_url, "/")
    error_message = "octopus_url must start with https:// and have no trailing slash (E21)."
  }
}

variable "octopus_space_slug" {
  description = "Slug of the platform space (<octopus-space-slug>); the first segment of every Octopus OIDC subject."
  type        = string
}

variable "acr_name" {
  description = "Globally unique ACR name (<acr-name>); login server <acr-name>.azurecr.io."
  type        = string
}

variable "acr_sku" {
  description = "ACR SKU. Standard unless private endpoints are required (Q6), which needs Premium."
  type        = string
  default     = "Standard"

  validation {
    condition     = contains(["Standard", "Premium"], var.acr_sku)
    error_message = "acr_sku must be Standard or Premium; Basic lacks the token and scope-map limits this design needs."
  }
}

variable "tfstate_storage_account_name" {
  description = "Terraform state storage account (<tfstate-storage-account>) in rg-workorders-shared."
  type        = string
}

variable "state_allowed_ip_ranges" {
  description = "Public ranges allowed to reach the state account: Octopus Cloud dynamic-worker egress (<octopus-cloud-static-ips>, Q7) and operator ranges. Empty keeps the firewall open; access still needs Entra RBAC."
  type        = list(string)
  default     = []
}

variable "state_operator_object_ids" {
  description = "Object IDs of the people or groups who run terraform/foundation. They get Storage Blob Data Contributor on the tfstate container (shared keys are disabled)."
  type        = list(string)
  default     = []
}

variable "state_path_conditions_enabled" {
  description = "Limit each provisioning identity's Storage Blob Data Contributor grant to its own state blob (environment-<class>.tfstate) with an ABAC condition, so nonprod identities cannot read or alter prod or foundation state."
  type        = bool
  default     = true
}

variable "networks" {
  description = "Address plan per cluster class. Pods use the Azure CNI overlay range set in terraform/environment, so the node subnet only holds nodes."
  type = map(object({
    address_space            = list(string)
    aks_nodes_prefix         = string
    private_endpoints_prefix = string
  }))

  validation {
    condition     = length(setsubtract(toset(keys(var.networks)), toset(["nonprod", "prod"]))) == 0 && length(var.networks) == 2
    error_message = "networks needs exactly the keys nonprod and prod."
  }
}

variable "log_retention_days" {
  description = "Interactive retention for log-workorders. Kube audit, Key Vault and SQL audit logs are the break-glass evidence, so keep at least 90 days."
  type        = number
  default     = 90
}

variable "provisioner_object_id" {
  description = "Object ID of the service principal behind the stored Octopus account 'Azure Runtime Provisioner'. Null skips its extra grants. The secret itself is never an input here."
  type        = string
  default     = null
}

variable "provisioner_grants_enabled" {
  description = "Keep the interim grants of the stored provisioner (ADR-C10). Set false at the phase-2 exit, when infra-nonprod switches to azure-oidc-env-lifecycle-nonprod (R4)."
  type        = bool
  default     = true
}

variable "deploy_identity_sql_db_contributor_envs" {
  description = "Environments whose id-octopus-deploy-<env> gets SQL DB Contributor on rg-workorders-<env>. §5.2 grants prod only; the uat runbooks db-backup and db-restore-pitr (§7.2) also need it (conflict reported to the chief architect)."
  type        = list(string)
  default     = ["prod"]

  validation {
    condition     = length(setsubtract(toset(var.deploy_identity_sql_db_contributor_envs), toset(["tdd", "uat", "prod"]))) == 0
    error_message = "Only tdd, uat and prod are valid."
  }
}

variable "create_migrator_identities" {
  description = "Create id-workorders-<env>-migrator (phase 4, after WI-05)."
  type        = bool
  default     = false
}

variable "lifecycle_node_rg_contributor_enabled" {
  description = "Q17 fallback: grant id-env-lifecycle-<class> Contributor on the AKS node resource group MC_<rg>_<cluster>_<region>. The group exists only after the first cluster build, so the Owner re-applies with true only if the phase-2 spike needs it."
  type        = bool
  default     = false
}

variable "lifecycle_workspace_reader_enabled" {
  description = "Grant id-env-lifecycle-<class> Reader on log-workorders, in case linking App Insights or diagnostic settings to a workspace in another resource group needs read access there [VERIFY in the phase-2 spike]."
  type        = bool
  default     = false
}

variable "sql_admin_group_display_names" {
  description = "Entra security groups that become the SQL Entra admin per class (<sql-admins-{class}>)."
  type        = map(string)

  validation {
    condition     = length(setsubtract(toset(keys(var.sql_admin_group_display_names)), toset(["nonprod", "prod"]))) == 0 && length(var.sql_admin_group_display_names) == 2
    error_message = "sql_admin_group_display_names needs exactly the keys nonprod and prod."
  }
}

variable "argocd_sso_app_display_name" {
  description = "Display name of the Argo CD SSO app registration (<argocd-sso-app>)."
  type        = string
}

variable "argocd_sso_redirect_uris" {
  description = "Argo CD callback URLs, one per instance: https://<argocd-{cluster}-host>/auth/callback (same host as configs.cm.url in argocd/bootstrap/values-<cluster>.yaml)."
  type        = list(string)
  default     = []
}

variable "argocd_sso_group_object_ids" {
  description = "Entra groups allowed to sign in to Argo CD (assignment required). Use the groups named in argocd/bootstrap/values-<cluster>.yaml policy.csv."
  type        = list(string)
  default     = []
}

variable "aks_oidc_issuer_urls" {
  description = "Cluster OIDC issuer per class, copied from the environment layer output after each cluster build. Used for the Argo CD SSO federated credential and to pin the federated-credential issuer policy to exact clusters. Empty before the first cluster exists."
  type        = map(string)
  default     = {}

  validation {
    condition     = length(setsubtract(toset(keys(var.aks_oidc_issuer_urls)), toset(["nonprod", "prod"]))) == 0
    error_message = "aks_oidc_issuer_urls accepts only the keys nonprod and prod."
  }
}

variable "fic_issuer_policy_effect" {
  description = "Effect of the preview built-in policy 'Managed Identity Federated Credentials should be from allowed issuer types'. Audit until the phase-2 spike proves no false positives, then Deny."
  type        = string
  default     = "Audit"

  validation {
    condition     = contains(["Audit", "Deny", "Disabled"], var.fic_issuer_policy_effect)
    error_message = "fic_issuer_policy_effect must be Audit, Deny or Disabled."
  }
}

variable "legacy_resource_group_ids" {
  description = "Resource IDs of the legacy Container Apps resource groups that get a CanNotDelete lock (R6). IDs only, supplied in the untracked tfvars; names from the inventory never enter this repository."
  type        = list(string)
  default     = []
}

variable "breakglass_group_object_id" {
  description = "Entra group of team SRE On-call. Gets PIM-eligible (not standing) AKS RBAC Cluster Admin and Key Vault Secrets Officer on the cluster and environment resource groups, used by docs/runbooks/break-glass.md and credential-rotation.md. Null skips it."
  type        = string
  default     = null
}

variable "security_alert_email_receivers" {
  description = "Receivers of security alerts (lock deletion, role-assignment writes, provisioner sign-ins): name => e-mail address."
  type        = map(string)
  default     = {}
}

variable "entra_signin_logs_enabled" {
  description = "Route Entra service-principal sign-in logs to log-workorders and alert on the stored provisioner's sign-ins (ADR-C10 recommendation 4). Needs an Entra role that can write tenant diagnostic settings [VERIFY], so it is off by default."
  type        = bool
  default     = false
}

variable "tags" {
  description = "Tags applied to every resource."
  type        = map(string)
  default = {
    "app"        = "workorders"
    "managed-by" = "terraform-foundation"
  }
}

locals {
  classes = ["nonprod", "prod"]

  # Application environment => cluster class (§7.2, §7.4).
  env_class = {
    tdd  = "nonprod"
    uat  = "nonprod"
    prod = "prod"
  }
  envs = keys(local.env_class)

  # Resource groups (§7.1).
  rg_shared = "rg-workorders-shared"
  rg_aks    = { for c in local.classes : c => "rg-workorders-aks-${c}" }
  rg_env    = { for e in local.envs : e => "rg-workorders-${e}" }
  resource_group_names = concat(
    [local.rg_shared],
    [for c in local.classes : local.rg_aks[c]],
    [for e in local.envs : local.rg_env[e]],
  )

  # Cluster names (§7.1). The environment layer creates the clusters.
  cluster_name = { for c in local.classes : c => "aks-workorders-${c}" }

  # Octopus OIDC issuer: the Octopus URL without a trailing slash (E21).
  octopus_issuer = trimsuffix(var.octopus_url, "/")

  federation_audience = "api://AzureADTokenExchange"
}
