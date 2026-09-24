# Inputs. Values live in an untracked terraform.tfvars (see terraform.tfvars.example); credentials only in TF_VAR_*.

# --- Octopus connection -------------------------------------------------------------------------------------

variable "octopus_url" {
  type        = string
  description = "Octopus Cloud URL, <OCTOPUS_URL>, without a trailing slash."
}

variable "octopus_space_id" {
  type        = string
  description = "ID of the platform space <octopus-space> (<octopus-space-id>)."
}

variable "octopus_space_slug" {
  type        = string
  description = "Slug of the platform space (<octopus-space-slug>). Used only to render the OIDC subjects in outputs."
}

variable "octopus_access_token" {
  type        = string
  description = "Octopus OIDC access token. Set TF_VAR_octopus_access_token; never write it to a file."
  default     = null
  sensitive   = true
}

variable "octopus_api_key" {
  type        = string
  description = "Personal Octopus API key of the platform engineer, if no access token is used. Set TF_VAR_octopus_api_key."
  default     = null
  sensitive   = true
}

# --- Azure identities created by terraform/foundation (§5.2) ------------------------------------------------

variable "azure_tenant_id" {
  type        = string
  description = "<AZURE_TENANT_ID>."
}

variable "azure_subscription_id" {
  type        = string
  description = "<AZURE_SUBSCRIPTION_ID>."
}

variable "deploy_identity_client_ids" {
  type        = map(string)
  description = "Client IDs of the UAMIs id-octopus-deploy-{tdd,uat,prod} (terraform/foundation outputs), keyed by environment."

  validation {
    condition     = length(setsubtract(toset(["tdd", "uat", "prod"]), toset(keys(var.deploy_identity_client_ids)))) == 0 && length(var.deploy_identity_client_ids) == 3
    error_message = "Provide exactly the keys tdd, uat and prod."
  }
}

variable "lifecycle_identity_client_ids" {
  type        = map(string)
  description = "Client IDs of the UAMIs id-env-lifecycle-{nonprod,prod} (terraform/foundation outputs), keyed by class."

  validation {
    condition     = length(setsubtract(toset(["nonprod", "prod"]), toset(keys(var.lifecycle_identity_client_ids)))) == 0 && length(var.lifecycle_identity_client_ids) == 2
    error_message = "Provide exactly the keys nonprod and prod."
  }
}

variable "acr_login_server" {
  type        = string
  description = "ACR login server, <acr-name>.azurecr.io."
}

variable "acr_pull_identity_client_id" {
  type        = string
  description = "Client ID of the UAMI id-octopus-acr-pull (AcrPull), used by feed acr-workorders over OIDC."
}

# --- Stored objects: looked up by name, never created or managed (§5.3) --------------------------------------

variable "stored_git_credential_name" {
  type        = string
  description = "Name of the stored Git credential used for config-as-code and pin commits."
  default     = "GitHub clearmeasure-aisf-sample-apps"
}

variable "stored_azure_account_name" {
  type        = string
  description = "Name of the stored client-secret account (Contributor). Referenced only by Azure.LifecycleAccount in infra-nonprod."
  default     = "Azure Runtime Provisioner"
}

variable "stored_library_variable_set_names" {
  type        = list(string)
  description = "Stored library variable sets that must stay included in no project."
  default     = ["Azure Runtime Provisioning", "GitHub AISF Sample Apps"]
}

# --- Projects and lifecycles ----------------------------------------------------------------------------------

variable "env_repo_url" {
  type        = string
  description = "<ENV_REPO_URL>: the environment repo that holds .octopus/ config-as-code and the GitOps pins."
  default     = "https://github.com/clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh.git"
}

variable "tdd_auto_deploy" {
  type        = bool
  description = "Phase TDD of workorders-standard deploys automatically. false in phase 1; true from phase 2 (§9)."
  default     = false
}

variable "default_channel_import_id" {
  type        = string
  description = "ID of the Default channel that Octopus creates with project workorders (for example Channels-123). Null on the first apply; set it for the second apply to adopt the channel."
  default     = null
}

variable "runbook_triggers_enabled" {
  type        = bool
  description = "Create the scheduled runbook triggers. Enable once the runbook OCL files are on main."
  default     = false
}

variable "runbook_trigger_timezone" {
  type        = string
  description = "Timezone of the scheduled runbook triggers."
  default     = "UTC"
}

# --- Library variable set `WorkOrders Environment` (§7.2) ----------------------------------------------------

variable "workorders_environment" {
  description = "Per-environment values for library variable set WorkOrders Environment. Keys: tdd, uat, prod. App.InternalUrl, Azure.ResourceGroup, Sql.ServerFqdn and the SQL user names follow §7.2 and are derived."
  type = map(object({
    app_base_url    = string # https://<{env}-hostname>
    key_vault_name  = string # <kv-workorders-{env}>
    sql_server_name = string # <sql-workorders-{env}>
    sql_database    = string # <sqldb-workorders-{env}>
    ai_openai_url   = string # <azure-openai-endpoint>
    ai_openai_model = string # <model-deployment-name>
  }))

  validation {
    condition     = length(setsubtract(toset(["tdd", "uat", "prod"]), toset(keys(var.workorders_environment)))) == 0 && length(var.workorders_environment) == 3
    error_message = "Provide exactly the keys tdd, uat and prod."
  }

  validation {
    condition     = alltrue([for v in values(var.workorders_environment) : startswith(v.app_base_url, "https://")])
    error_message = "app_base_url must start with https://."
  }
}

# --- Library variable set `WorkOrders Infrastructure` (§7.2) -------------------------------------------------

variable "workorders_infrastructure" {
  description = "Values for library variable set WorkOrders Infrastructure. Environment.Class and Terraform.StateKey are derived per infra environment."
  type = object({
    state_resource_group  = optional(string, "rg-workorders-shared")
    state_storage_account = string # <tfstate-storage-account>
    state_container       = optional(string, "tfstate")
  })
}

# --- People and Codefresh -------------------------------------------------------------------------------------

variable "team_external_groups" {
  type        = map(list(string))
  description = "External security group IDs (for example Entra group object IDs) per team name. Membership is managed in the identity provider."
  default     = {}
}

variable "team_member_user_ids" {
  type        = map(list(string))
  description = "Octopus user IDs per team name, for members not managed through external groups."
  default     = {}
}

variable "codefresh_account_id" {
  type        = string
  description = "<CF_ACCOUNT_ID>."
}

variable "codefresh_release_pipeline_id" {
  type        = string
  description = "<CF_RELEASE_PIPELINE_ID> of workorders/release."
}

variable "codefresh_oidc_subject" {
  type        = string
  description = "Exact OIDC subject for identity codefresh-release-master. Null renders the §7.2 pattern; replace it with the sub copied from a test build, wildcarding only the user segment [VERIFY]."
  default     = null
}

variable "ci_release_publisher_tdd_deploy" {
  type        = bool
  description = "Q3: grant CI Release Publishers the built-in Deployment Creator role, scoped to workorders and tdd, only if lifecycle auto-deploy needs it."
  default     = false
}

# --- Prod freeze ------------------------------------------------------------------------------------------------

variable "prod_freeze_first_window" {
  type = object({
    start = string
    end   = string
  })
  description = "First occurrence of prod-weekend-freeze (RFC 3339): a Saturday 00:00 to the following Monday 00:00 in the team's timezone. It repeats weekly."
  default = {
    start = "2026-10-03T00:00:00Z"
    end   = "2026-10-05T00:00:00Z"
  }
}
