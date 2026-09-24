# Feeds (§7.2, §7.5). The built-in feed (slug octopus-server-built-in) holds ChurchBulletin.Database and
# ChurchBulletin.AcceptanceTests; Codefresh pushes them with svc-codefresh-release.
#
# acr-workorders: Azure Container Registry feed over OIDC (E30, E38); no stored credential. Octopus reads
# workorders/ui-server and workorders/worker versions for release creation and the Argo CD step, and the Kubernetes
# workers pull platform/ci-dotnet step images (the node's kubelet identity also holds AcrPull, §5.2).
# Federated subject for id-octopus-acr-pull, created by terraform/foundation: space:<space-slug>:feed:acr-workorders
# [VERIFY format]; outputs.tf renders it for cross-checking.
#
# [CONTRACT GAP] The worker-tools execution container of the Terraform steps (octopusdeploy/worker-tools on
# Hosted Ubuntu, §7.2) needs a Docker Hub feed. §7.2 names none and the platform space has only built-in feeds;
# the runbooks mark it <docker-hub-feed>. Nothing is created here until the name is decided.

resource "octopusdeploy_azure_container_registry" "acr_workorders" {
  name                           = "acr-workorders"
  feed_uri                       = "https://${var.acr_login_server}"
  download_attempts              = 3
  download_retry_backoff_seconds = 10

  oidc_authentication = {
    client_id    = var.acr_pull_identity_client_id
    tenant_id    = var.azure_tenant_id
    audience     = "api://AzureADTokenExchange"
    subject_keys = ["space", "feed"]
  }
}
