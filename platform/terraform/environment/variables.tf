# Inputs for one cluster class. The runbooks run Terraform from the project Git repository and
# pass -var-file=#{Environment.Class}.tfvars, so <class>.tfvars (from <class>.tfvars.example and
# the foundation output environment_inputs[<class>]) is committed next to this file in the
# environment repository. It holds identifiers only. The sensitive, ephemeral variables at the end
# of this file come from Octopus sensitive variables as TF_VAR_<name> environment variables.

variable "cluster" {
  description = "Cluster class: nonprod (tdd, uat) or prod (prod). Selects the environments, the SKU tier and argocd/bootstrap/*-<cluster>.yaml."
  type        = string

  validation {
    condition     = contains(["nonprod", "prod"], var.cluster)
    error_message = "cluster must be nonprod or prod."
  }
}

variable "tenant_id" {
  description = "Entra tenant ID (<AZURE_TENANT_ID>)."
  type        = string
}

variable "subscription_id" {
  description = "Subscription ID (<AZURE_SUBSCRIPTION_ID>)."
  type        = string
}

variable "location" {
  description = "Azure region (<azure-region>); same as the foundation."
  type        = string
}

# --- Foundation outputs (environment_inputs[<class>]) ---------------------------------------------

variable "aks_controlplane_identity_id" {
  description = "Resource ID of id-aks-<class>-controlplane."
  type        = string
}

variable "aks_kubelet_identity_id" {
  description = "Resource ID of id-aks-<class>-kubelet."
  type        = string
}

variable "aks_nodes_subnet_id" {
  description = "Resource ID of snet-aks-nodes in vnet-workorders-<class>."
  type        = string
}

variable "private_endpoints_subnet_id" {
  description = "Resource ID of snet-private-endpoints in vnet-workorders-<class>."
  type        = string
}

variable "private_dns_zone_ids" {
  description = "Private DNS zones of the class for SQL and Key Vault."
  type = object({
    sql       = string
    key_vault = string
  })
}

variable "log_analytics_workspace_id" {
  description = "Resource ID of log-workorders."
  type        = string
}

variable "sql_admin_group" {
  description = "SQL Entra admin group <sql-admins-{class}>: display name and object ID."
  type = object({
    display_name = string
    object_id    = string
  })
}

variable "workload_identity_ids" {
  description = "Resource IDs of id-workorders-<env>-app and id-workorders-<env>-eso, keyed by environment."
  type = map(object({
    app = string
    eso = string
  }))

  validation {
    condition     = alltrue([for e in(var.cluster == "nonprod" ? ["tdd", "uat"] : ["prod"]) : contains(keys(var.workload_identity_ids), e)])
    error_message = "workload_identity_ids needs one entry per environment of the class."
  }
}

variable "platform_identity_ids" {
  description = "Resource IDs of id-eso-platform-<cluster> and id-kyverno-<cluster>."
  type = object({
    eso     = string
    kyverno = string
  })
}

# --- Names (§7.1; globally unique names stay placeholders in the repository) --------------------

variable "key_vault_names" {
  description = "Per-environment vault names <kv-workorders-{env}>."
  type        = map(string)

  validation {
    condition     = alltrue([for e in(var.cluster == "nonprod" ? ["tdd", "uat"] : ["prod"]) : contains(keys(var.key_vault_names), e)])
    error_message = "key_vault_names needs one entry per environment of the class."
  }
}

variable "platform_key_vault_name" {
  description = "Platform vault of the cluster (<kv-workorders-platform-{cluster}>)."
  type        = string
}

variable "sql_server_names" {
  description = "Per-environment SQL logical servers <sql-workorders-{env}>."
  type        = map(string)

  validation {
    condition     = alltrue([for e in(var.cluster == "nonprod" ? ["tdd", "uat"] : ["prod"]) : contains(keys(var.sql_server_names), e)])
    error_message = "sql_server_names needs one entry per environment of the class."
  }
}

variable "sql_database_names" {
  description = "Per-environment databases <sqldb-workorders-{env}>."
  type        = map(string)

  validation {
    condition     = alltrue([for e in(var.cluster == "nonprod" ? ["tdd", "uat"] : ["prod"]) : contains(keys(var.sql_database_names), e)])
    error_message = "sql_database_names needs one entry per environment of the class."
  }
}

# --- Cluster ---------------------------------------------------------------------------------------

variable "kubernetes_version" {
  description = "AKS minor version, for example 1.34. Null takes the current default at creation; the patch channel keeps it patched."
  type        = string
  default     = null
}

variable "system_node_pool" {
  description = "Default pool 'system' (critical add-ons only)."
  type = object({
    vm_size   = string
    min_count = number
    max_count = number
  })
  default = {
    vm_size   = "Standard_D4ds_v5"
    min_count = 1
    max_count = 3
  }
}

variable "apps_node_pool" {
  description = "User pool 'apps' (workorders, add-ons, Octopus workers)."
  type = object({
    vm_size   = string
    min_count = number
    max_count = number
  })
  default = {
    vm_size   = "Standard_D4ds_v5"
    min_count = 1
    max_count = 4
  }
}

variable "cluster_network" {
  description = "Azure CNI overlay ranges. Must not overlap the VNet or any peered network."
  type = object({
    pod_cidr       = string
    service_cidr   = string
    dns_service_ip = string
  })
  default = {
    pod_cidr       = "192.168.0.0/16"
    service_cidr   = "172.16.0.0/16"
    dns_service_ip = "172.16.0.10"
  }
}

variable "api_server_authorized_ip_ranges" {
  description = "Public ranges allowed to reach the API server: Octopus Cloud dynamic workers (<octopus-cloud-static-ips>, Q7) and break-glass operators. Empty leaves the endpoint open; Entra RBAC still applies."
  type        = list(string)
  default     = []
}

# --- Data services ---------------------------------------------------------------------------------

variable "key_vault_allowed_ip_ranges" {
  description = "Public ranges allowed to the vault data planes (Octopus Cloud dynamic workers write secrets from env-apply). Workloads use private endpoints. Empty leaves the firewall open; RBAC still applies."
  type        = list(string)
  default     = []
}

variable "sql_databases" {
  description = "Per-environment database settings. prod needs 35-day point-in-time restore (R18)."
  type = map(object({
    sku_name                  = string
    pitr_days                 = number
    backup_storage_redundancy = string
    ltr_weekly                = optional(string)
    ltr_monthly               = optional(string)
    ltr_yearly                = optional(string)
    ltr_week_of_year          = optional(number)
  }))

  validation {
    condition     = alltrue([for e in(var.cluster == "nonprod" ? ["tdd", "uat"] : ["prod"]) : contains(keys(var.sql_databases), e)])
    error_message = "sql_databases needs one entry per environment of the class."
  }
}

# --- Monitoring (ADR-D15) --------------------------------------------------------------------------

variable "oncall_email_receivers" {
  description = "Receivers of the SLO alerts of this class: name => e-mail address."
  type        = map(string)
  default     = {}
}

variable "slo_alert_enabled" {
  description = "Enable the fast-burn SLO alert. The alerts go live in phase 3 (§9)."
  type        = bool
  default     = true
}

# --- Octopus Kubernetes workers (ADR-D14) ----------------------------------------------------------

variable "octopus_url" {
  description = "Octopus Cloud URL (<OCTOPUS_URL>), no trailing slash."
  type        = string
}

variable "octopus_space" {
  description = "Name of the platform space (<octopus-space>); the chart registers the worker there."
  type        = string
}

variable "octopus_worker_chart_version" {
  description = "Initial version of the Octopus kubernetes-agent chart (<kubernetes-agent-chart-version>; 3.15.1 on 2026-09-24). Octopus upgrades the worker afterwards, so later changes are ignored."
  type        = string
}

# --- Argo CD bootstrap (ADR-D3) --------------------------------------------------------------------

variable "argocd_chart_version" {
  description = "argo/argo-cd chart version (<argo-cd-chart-version>; 10.9.2 carries app v3.5.3). Must equal the pin in argocd/clusters/<cluster>/addons/argocd.yaml so self-management does not change the version."
  type        = string
}

variable "argocd_apps_chart_version" {
  description = "argo/argocd-apps chart version (<argocd-apps-chart-version>; 2.0.5 on 2026-09-24)."
  type        = string
}

variable "env_repo_url" {
  description = "Environment repository (<ENV_REPO_URL>)."
  type        = string
}

variable "argocd_repo_private" {
  description = "The environment repo needs a credential (R2 recommends private). Keep this constant across applies: switching it to false deletes the bootstrap Secret."
  type        = bool
  default     = true
}

variable "kubelogin_login_mode" {
  description = "kubelogin login mode for the Kubernetes and Helm providers (providers.tf)."
  type        = string
  default     = "azurecli"

  validation {
    condition     = contains(["azurecli", "spn", "workloadidentity", "msi"], var.kubelogin_login_mode)
    error_message = "kubelogin_login_mode must be azurecli, spn, workloadidentity or msi."
  }
}

variable "tags" {
  description = "Tags applied to every resource."
  type        = map(string)
  default = {
    "app"        = "workorders"
    "managed-by" = "terraform-environment"
  }
}

# --- Sensitive, ephemeral inputs -------------------------------------------------------------------
# Ephemeral variables never enter the plan or the state; they feed write-only arguments only.
# Supply them on each run that installs something, from Octopus sensitive variables.

variable "octopus_worker_registration_token" {
  description = "Octopus.WorkerRegistrationToken (TF_VAR_octopus_worker_registration_token): short-lived bearer token that registers the Kubernetes workers (ADR-D14). Needed only on the run that installs or replaces a worker; null or empty sends nothing."
  type        = string
  default     = null
  sensitive   = true
  ephemeral   = true
}

variable "argocd_repo_read_credential" {
  description = "Read-only credential for the environment repo, used once to seed Secret argocd-repo-creds before ESO exists (§5.2). The same JSON object as Key Vault secret argocd-repo-read-credential: {\"username\": ..., \"password\": ...} for a fine-grained token or {\"githubAppID\": ..., \"githubAppInstallationID\": ..., \"githubAppPrivateKey\": ...} for a GitHub App (R11). Never the stored org-wide PAT. Null or empty skips it."
  type        = string
  default     = null
  sensitive   = true
  ephemeral   = true
}

locals {
  envs = var.cluster == "nonprod" ? ["tdd", "uat"] : ["prod"]

  rg_aks = "rg-workorders-aks-${var.cluster}"
  rg_env = { for e in local.envs : e => "rg-workorders-${e}" }

  cluster_name = "aks-workorders-${var.cluster}"

  # ADR-D1: nonprod Free tier, prod Standard tier (uptime SLA).
  aks_sku_tier = var.cluster == "prod" ? "Standard" : "Free"

  federation_audience = "api://AzureADTokenExchange"
}
