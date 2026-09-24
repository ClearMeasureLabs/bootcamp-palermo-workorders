# Entra objects (§7.10): the SQL admin groups and the Argo CD SSO app registration. Applied by
# an Entra administrator (Groups Administrator and Application Administrator, §5.2) together
# with, or right after, the Azure Owner; the azuread provider uses the operator's own sign-in.

# --- SQL Entra admin groups <sql-admins-{class}> -------------------------------------------------
# The group is the Entra admin of every SQL server of its class (terraform/environment/
# data-services.tf). Members: id-env-lifecycle-<class>, which runs configure-db-principals-<env>
# (CREATE USER ... FROM EXTERNAL PROVIDER WITH OBJECT_ID, no Graph permission needed), and, in
# nonprod during phases 1-2, the stored provisioner.
# Group membership of a managed identity can take about 24 hours to reach its tokens: apply this
# at least a day before the first env-apply of the class.

resource "azuread_group" "sql_admins" {
  for_each = var.sql_admin_group_display_names

  display_name            = each.value
  description             = "Entra admin of the workorders ${each.key} SQL servers (terraform/foundation)."
  security_enabled        = true
  prevent_duplicate_names = true
}

resource "azuread_group_member" "sql_admins_lifecycle" {
  for_each = toset(local.classes)

  group_object_id  = azuread_group.sql_admins[each.key].object_id
  member_object_id = azurerm_user_assigned_identity.env_lifecycle[each.key].principal_id
}

resource "azuread_group_member" "sql_admins_provisioner" {
  count = local.provisioner_active ? 1 : 0

  group_object_id  = azuread_group.sql_admins["nonprod"].object_id
  member_object_id = var.provisioner_object_id
}

# --- Argo CD SSO <argocd-sso-app> ---------------------------------------------------------------
# One registration for both instances. Argo CD signs users in with OIDC and proves its own
# identity with workload identity federation (Q15 default, no client secret): the argocd-server
# service account of each cluster federates to this application.
# https://argo-cd.readthedocs.io/en/stable/operator-manual/user-management/microsoft/

data "azuread_application_published_app_ids" "well_known" {}

data "azuread_service_principal" "msgraph" {
  client_id = data.azuread_application_published_app_ids.well_known.result["MicrosoftGraph"]
}

resource "azuread_application" "argocd_sso" {
  display_name     = var.argocd_sso_app_display_name
  sign_in_audience = "AzureADMyOrg"

  # Only groups assigned to the application appear in the token; this matches
  # requestedIDTokenClaims.groups.value "ApplicationGroup" in the Argo CD values and avoids
  # group overage.
  group_membership_claims = ["ApplicationGroup"]

  web {
    redirect_uris = var.argocd_sso_redirect_uris

    implicit_grant {
      access_token_issuance_enabled = false
      id_token_issuance_enabled     = false
    }
  }

  optional_claims {
    id_token {
      name = "groups"
    }
  }

  required_resource_access {
    resource_app_id = data.azuread_application_published_app_ids.well_known.result["MicrosoftGraph"]

    resource_access {
      id   = data.azuread_service_principal.msgraph.oauth2_permission_scope_ids["User.Read"]
      type = "Scope"
    }
  }
}

resource "azuread_service_principal" "argocd_sso" {
  client_id = azuread_application.argocd_sso.client_id

  # Only assigned groups may sign in.
  app_role_assignment_required = true
}

resource "azuread_app_role_assignment" "argocd_sso" {
  for_each = toset(var.argocd_sso_group_object_ids)

  # Default access role.
  app_role_id         = "00000000-0000-0000-0000-000000000000"
  principal_object_id = each.key
  resource_object_id  = azuread_service_principal.argocd_sso.object_id
}

# One credential per cluster issuer. The issuer changes when a cluster is rebuilt, so the
# Entra administrator re-applies with the new aks_oidc_issuer_urls value (environment output
# oidc_issuer_url).
resource "azuread_application_federated_identity_credential" "argocd_server" {
  for_each = var.aks_oidc_issuer_urls

  application_id = azuread_application.argocd_sso.id
  display_name   = "argocd-server-${each.key}"
  description    = "Argo CD server of aks-workorders-${each.key}"
  audiences      = [local.federation_audience]
  issuer         = each.value
  subject        = "system:serviceaccount:argocd:argocd-server"
}
