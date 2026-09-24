# Terraform state for both layers: <tfstate-storage-account>, container tfstate (§7.2 variables
# Terraform.StateStorageAccount and Terraform.StateContainer).
#
# Keys: foundation.tfstate (this layer) and environment-<class>.tfstate (Octopus runbooks).
# Environment state holds secrets the providers cannot keep out (for example the App Insights
# connection string), so:
#   - shared keys are disabled; every reader authenticates with Entra ID and needs Storage Blob
#     Data Contributor on the container (role-assignments.tf);
#   - versioning and soft delete allow recovery from a corrupted or deleted state blob;
#   - blob reads and writes are logged to log-workorders (audit evidence in the runbooks);
#   - a CanNotDelete lock protects the account (governance.tf).

resource "azurerm_storage_account" "tfstate" {
  name                = var.tfstate_storage_account_name
  resource_group_name = azurerm_resource_group.this[local.rg_shared].name
  location            = var.location

  account_kind             = "StorageV2"
  account_tier             = "Standard"
  account_replication_type = "ZRS"

  shared_access_key_enabled         = false
  default_to_oauth_authentication   = true
  allow_nested_items_to_be_public   = false
  https_traffic_only_enabled        = true
  min_tls_version                   = "TLS1_2"
  infrastructure_encryption_enabled = true
  public_network_access             = "Enabled"

  network_rules {
    # Deny by default once the allowed ranges are known (Octopus Cloud egress, Q7).
    default_action = length(var.state_allowed_ip_ranges) > 0 ? "Deny" : "Allow"
    ip_rules       = var.state_allowed_ip_ranges
    bypass         = ["AzureServices"]
  }

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 30
    }

    container_delete_retention_policy {
      days = 30
    }
  }

  tags = var.tags
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}

# Who read or wrote which state blob, and when.
resource "azurerm_monitor_diagnostic_setting" "tfstate_blob" {
  name                       = "state-access-to-log-workorders"
  target_resource_id         = "${azurerm_storage_account.tfstate.id}/blobServices/default"
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id

  enabled_log {
    category = "StorageRead"
  }

  enabled_log {
    category = "StorageWrite"
  }

  enabled_log {
    category = "StorageDelete"
  }
}
