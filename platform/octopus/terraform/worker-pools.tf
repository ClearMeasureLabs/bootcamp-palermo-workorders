# Worker pools (§7.2, ADR-C2, ADR-D14).
#
# Static pools k8s-tdd, k8s-uat and k8s-prod hold one Kubernetes worker each. terraform/environment installs the
# workers as helm_release octopus-worker-<env> in namespace octopus-worker-<env> and registers them into these
# pools with the short-lived token Octopus.WorkerRegistrationToken; Octopus upgrades them afterwards. A Kubernetes
# worker "is limited to modifying its local namespace" (E27), so it reaches SQL, Key Vault and ui-server:8080
# without write access to Argo-managed namespaces.
#
# The built-in dynamic pool Hosted Ubuntu (slug hosted-ubuntu) runs the Terraform steps; it is looked up only to
# prove that the slug used by the runbooks exists.

resource "octopusdeploy_static_worker_pool" "k8s" {
  for_each = toset(local.app_environments)

  name        = "k8s-${each.key}"
  description = "Kubernetes worker in namespace octopus-worker-${each.key} on aks-workorders-${each.key == "prod" ? "prod" : "nonprod"}. Runs ${each.key} steps and ${each.key == "prod" ? "infra-prod" : "infra-nonprod"} database-principal steps."
  is_default  = false
}

data "octopusdeploy_worker_pools" "hosted_ubuntu" {
  partial_name = "Hosted Ubuntu"
  take         = 10

  lifecycle {
    postcondition {
      condition     = length([for p in self.worker_pools : p if p.name == "Hosted Ubuntu"]) == 1
      error_message = "The built-in dynamic worker pool 'Hosted Ubuntu' (slug hosted-ubuntu) is missing from the platform space."
    }
  }
}
