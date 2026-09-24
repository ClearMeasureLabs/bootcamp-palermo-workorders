# Channels of project workorders (§7.2). Channels are not stored in Git (E26).
#
# Version rule: every package version has no pre-release tag (tag = "^$"). Rules reference the step slugs and
# package-reference names of .octopus/workorders/deployment_process.ocl [VERIFY slug vs name for
# version-controlled processes]. Git reference rule refs/heads/main: releases pass GIT_REF refs/heads/main
# and no GIT_COMMIT (§7.7, ADR-D7).
#
# Who may create releases: Octopus scopes ReleaseCreate by project and environment, not by channel. "Default
# releases only by svc-codefresh-release" and "Hotfix releases by Release Managers" are therefore procedural,
# audited through the release's creator; Release Managers hold Release Creator on workorders (teams.tf).

locals {
  channel_package_rules = [
    { deployment_action = "migrate-database", package_reference = "ChurchBulletin.Database" },
    { deployment_action = "update-argo-cd-image-tags", package_reference = "ui-server" },
    { deployment_action = "update-argo-cd-image-tags", package_reference = "worker" },
    { deployment_action = "acceptance-tests", package_reference = "ChurchBulletin.AcceptanceTests" },
  ]
}

# Octopus creates a channel named "Default" with every project, so this configuration adopts it instead of
# creating a duplicate: apply once with default_channel_import_id = null, read the ID of the Default channel of
# project workorders, set the variable and apply again. No release may be created in between.
resource "octopusdeploy_channel" "default" {
  count = var.default_channel_import_id == null ? 0 : 1

  name                = "Default"
  description         = "Releases created by Codefresh workorders/release (svc-codefresh-release), release number = package version. Lifecycle workorders-standard."
  project_id          = octopusdeploy_project.workorders.id
  lifecycle_id        = octopusdeploy_lifecycle.workorders_standard.id
  is_default          = true
  git_reference_rules = ["refs/heads/main"]

  rule {
    tag = "^$"

    dynamic "action_package" {
      for_each = local.channel_package_rules
      content {
        deployment_action = action_package.value.deployment_action
        package_reference = action_package.value.package_reference
      }
    }
  }
}

import {
  for_each = var.default_channel_import_id == null ? toset([]) : toset([var.default_channel_import_id])
  to       = octopusdeploy_channel.default[0]
  id       = each.value
}

resource "octopusdeploy_channel" "hotfix" {
  name                = "Hotfix"
  description         = "Created by Release Managers. Release number <package-version>-hotfix.<n>. Lifecycle workorders-hotfix (UAT, Prod); step hotfix-justification runs first."
  project_id          = octopusdeploy_project.workorders.id
  lifecycle_id        = octopusdeploy_lifecycle.workorders_hotfix.id
  is_default          = false
  git_reference_rules = ["refs/heads/main"]

  # Hotfix packages are master builds too, so the same guard applies.
  rule {
    tag = "^$"

    dynamic "action_package" {
      for_each = local.channel_package_rules
      content {
        deployment_action = action_package.value.deployment_action
        package_reference = action_package.value.package_reference
      }
    }
  }
}
