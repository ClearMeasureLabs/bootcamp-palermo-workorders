# One-time in-cluster bootstrap (§7.3, §7.4, ADR-D3, ADR-D14).
#
# 1. Argo CD. Terraform installs the argo/argo-cd chart once as release `argocd` and the
#    argo/argocd-apps chart once as release `argocd-apps` (Application platform-root), with the
#    values files the gitops-architect package maintains:
#      argocd/bootstrap/values-<cluster>.yaml     (read by this file and by the self-managing
#                                                  add-on Application argocd/clusters/<cluster>/addons/argocd.yaml)
#      argocd/bootstrap/root-app-<cluster>.yaml
#    After the first sync Argo CD manages itself from Git, so both releases ignore every later
#    change (ignore_changes = all). Change Argo CD by pull request to argocd/**, never here.
#    The files are read verbatim: in the environment repository their placeholders hold real
#    values.
#
# 2. The environment-repo credential. ESO cannot run before Argo CD installs it, so Terraform
#    seeds Secret argocd/argocd-repo-creds once from the ephemeral variable
#    argocd_repo_read_credential (write-only data_wo: never in plan or state). ExternalSecret
#    argocd-repo-creds (creationPolicy Orphan) then adopts and refreshes it from
#    <kv-workorders-platform-{cluster}> (argocd/clusters/<cluster>/platform-secrets.yaml).
#
# 3. Octopus Kubernetes workers, one release per environment in namespace octopus-worker-<env>
#    (ADR-D14). Octopus upgrades them afterwards, so the chart version is ignored. The
#    registration token Octopus.WorkerRegistrationToken arrives through the ephemeral variable
#    octopus_worker_registration_token and the write-only set_wo argument, so it stays out of
#    plan and state (it does live in the in-cluster Helm release Secret, as it would with any
#    install). Re-register a worker by replacing its release:
#      terraform apply -replace='helm_release.octopus_worker["<env>"]' with a fresh token.
#    Hardening checked against chart kubernetes-agent 3.15.1 (`helm template`, 2026-09-24): with
#    default values the chart binds a cluster-wide `*` ClusterRole to the auto-upgrader service
#    account. useNamespacedRoles = true and clusterRole.enabled = false leave only Roles in
#    octopus-worker-<env>, matching "limited to modifying its local namespace" (E27).
#    [VERIFY] Octopus-driven upgrades still succeed with namespaced roles.

locals {
  argocd_values_file   = "${path.module}/../../argocd/bootstrap/values-${var.cluster}.yaml"
  argocd_root_app_file = "${path.module}/../../argocd/bootstrap/root-app-${var.cluster}.yaml"

  # Octopus Cloud polling endpoint: https://polling.<instance>.octopus.app/ [VERIFY for the
  # instance; the portal's worker installation command shows the exact value].
  octopus_polling_url = "${replace(var.octopus_url, "https://", "https://polling.")}/"

  platform_namespace_labels = {
    "tier"                               = "platform"
    "pod-security.kubernetes.io/enforce" = "baseline"
    "pod-security.kubernetes.io/warn"    = "restricted"
  }
}

# --- Argo CD ---------------------------------------------------------------------------------------

resource "kubernetes_namespace_v1" "argocd" {
  metadata {
    name   = "argocd"
    labels = local.platform_namespace_labels
  }

  depends_on = [azurerm_kubernetes_cluster_node_pool.apps]
}

resource "kubernetes_secret_v1" "argocd_repo_creds" {
  count = var.argocd_repo_private ? 1 : 0

  metadata {
    name      = "argocd-repo-creds"
    namespace = kubernetes_namespace_v1.argocd.metadata[0].name
    labels = {
      "argocd.argoproj.io/secret-type" = "repo-creds"
    }
  }

  # Credential-template fields for the repository prefix; either username/password or the
  # GitHub App fields (argocd/clusters/<cluster>/platform-secrets.yaml).
  data_wo = merge(
    var.argocd_repo_read_credential == null ? {} : var.argocd_repo_read_credential,
    {
      type = "git"
      url  = var.env_repo_url
    },
  )
  data_wo_revision = 1

  lifecycle {
    # ESO owns the content and adds its own metadata after the first refresh.
    ignore_changes = [metadata[0].labels, metadata[0].annotations]
  }
}

resource "helm_release" "argo_cd" {
  name       = "argocd"
  repository = "https://argoproj.github.io/argo-helm"
  chart      = "argo-cd"
  version    = var.argocd_chart_version
  namespace  = kubernetes_namespace_v1.argocd.metadata[0].name

  values = [file(local.argocd_values_file)]

  wait    = true
  timeout = 900

  depends_on = [kubernetes_secret_v1.argocd_repo_creds]

  lifecycle {
    # Argo CD manages this release after the first sync (addons/argocd.yaml).
    ignore_changes = all
  }
}

resource "helm_release" "argocd_apps" {
  name       = "argocd-apps"
  repository = "https://argoproj.github.io/argo-helm"
  chart      = "argocd-apps"
  version    = var.argocd_apps_chart_version
  namespace  = kubernetes_namespace_v1.argocd.metadata[0].name

  # Application platform-root and the bootstrap copy of AppProject platform-addons.
  values = [file(local.argocd_root_app_file)]

  depends_on = [helm_release.argo_cd]

  lifecycle {
    ignore_changes = all
  }
}

# --- Octopus Kubernetes workers (ADR-D14) ------------------------------------------------------------

resource "kubernetes_namespace_v1" "octopus_worker" {
  for_each = toset(local.envs)

  metadata {
    name = "octopus-worker-${each.key}"
    labels = merge(local.platform_namespace_labels, {
      "environment" = each.key
    })
  }

  depends_on = [azurerm_kubernetes_cluster_node_pool.apps]
}

resource "helm_release" "octopus_worker" {
  for_each = toset(local.envs)

  name       = "octopus-worker-${each.key}"
  repository = "oci://registry-1.docker.io/octopusdeploy"
  chart      = "kubernetes-agent"
  version    = var.octopus_worker_chart_version
  namespace  = kubernetes_namespace_v1.octopus_worker[each.key].metadata[0].name

  values = [yamlencode({
    agent = {
      # "Y" accepts the Octopus Customer Agreement, as the portal's install command does.
      acceptEula           = "Y"
      name                 = "octopus-worker-${each.key}"
      serverUrl            = "${var.octopus_url}/"
      serverCommsAddresses = [local.octopus_polling_url]
      space                = var.octopus_space
      worker = {
        enabled = true
        initial = {
          # Static Kubernetes worker pool of the environment (§7.2).
          workerPools = ["k8s-${each.key}"]
        }
      }
    }
    scriptPods = {
      serviceAccount = {
        useNamespacedRoles = true
        clusterRole = {
          enabled = false
        }
      }
    }
  })]

  # Registration token: write-only, sent on install (and on -replace).
  set_wo = var.octopus_worker_registration_token == null ? [] : [
    {
      name  = "agent.bearerToken"
      value = var.octopus_worker_registration_token
    },
  ]
  set_wo_revision = 1

  wait    = true
  timeout = 600

  lifecycle {
    # Octopus upgrades the worker (E30); Terraform must not roll the chart back.
    ignore_changes = [version]
  }
}
