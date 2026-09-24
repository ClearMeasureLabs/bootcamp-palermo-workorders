# Lab 22: Environment Lifecycle and On-Call Runbooks

**Curriculum Section:** Sections 06-07 (Operate/Execute & Reporting)
**Estimated Time:** 40 minutes
**Type:** Operate + Report
**Builds on:** Lab 10 (stability monitoring), Lab 21 (drift and rollback)

---

## Objective

Explain how an environment is created, changed, rebuilt and destroyed, who is allowed to do each, and which identity acts. Connect the environment runbooks to the on-call runbooks and to the delivery metrics that show whether the platform is healthy.

## Background

**Two Terraform layers** (ADR-D10), split by what a Contributor cannot do (E36):

| Layer | Applied by | Creates | Never |
|---|---|---|---|
| `terraform/foundation` (state `foundation.tfstate`) | A human Owner through PIM | Resource groups, networks, ACR, Log Analytics, Terraform state, every UAMI, **every role assignment**, Octopus-issuer federated credentials, locks, policies, Entra groups | Runs from a pipeline |
| `terraform/environment` (state `environment-{class}.tfstate`) | Octopus runbooks in project `workorders-infrastructure` | AKS, SQL, Key Vaults, App Insights, workload federated credentials, the one-time Argo CD bootstrap, Octopus workers | Contains `azurerm_role_assignment` (lint TB08) |

Grants are made at resource-group scope in advance, so resources the environment layer creates later inherit them, and identities survive a cluster rebuild.

**Runbooks** (project `workorders-infrastructure`, environments `infra-nonprod` and `infra-prod`):

| Runbook | Environments | What it does |
|---|---|---|
| `env-plan` | `infra-nonprod`, `infra-prod` | "Plan to apply a Terraform template" from `terraform/environment` in the project's Git repo; the plan is saved as an artifact; variable substitution in `.tf` files is off |
| `env-apply` | `infra-nonprod`, `infra-prod` | Plan → manual intervention (always) → apply → `configure-db-principals-<env>` on `k8s-<env>` |
| `env-destroy` | `infra-nonprod` **only** | Manual intervention → destroy the resources inside the resource groups, never the groups |
| `rotate-sql-passwords` | `infra-nonprod`, `infra-prod` | Monthly: new password → `ALTER USER` → Key Vault → verify |
| `provisioner-credential-check` | `infra-nonprod` | Daily: warns 14 days before `Provisioner.SecretExpiresOn`; `az login` smoke |

**Identity** (`Azure.LifecycleAccount`):

| Environment | Phases 1–2 | From the phase-2 exit |
|---|---|---|
| `infra-nonprod` | `azure-runtime-provisioner` (stored client secret, Contributor) | `azure-oidc-env-lifecycle-nonprod` → `id-env-lifecycle-nonprod` |
| `infra-prod` | `azure-oidc-env-lifecycle-prod` from its first run | Same |

**On-call runbooks** (project `workorders`): `db-backup` (`uat`, `prod`), `db-restore-pitr` (`uat`, `prod`), `run-acceptance-tests` (`tdd`). Human procedures are in [../runbooks/](../runbooks/): break-glass, rollback and forward fix, database restore (PITR), credential rotation, SLO fast burn.

## Steps (online)

A platform engineer drives; students observe the runbook runs in Octopus with the `Developers` role.

### Step 1: Plan

Run `env-plan` in `infra-nonprod`. Open the plan artifact. Classify each change: expected (a pull request changed `terraform/environment`), drift (someone changed Azure by hand), or surprise.

### Step 2: Apply

Run `env-apply` in `infra-nonprod`. Note where the run stops for the manual intervention and who approves it. After the apply, `configure-db-principals-tdd` and `configure-db-principals-uat` run on the environments' own workers.

### Step 3: Rebuild thought experiment

Suppose `aks-workorders-nonprod` must be rebuilt. List what the environment layer re-creates, what survives, and what people must redo. Then compare with the answer key.

### Step 4: Rotate and check credentials

Open the last `rotate-sql-passwords` and `provisioner-credential-check` runs. Where does each new password live, and which step reads it during the next deployment?

### Step 5: Read the health signals

- The fast-burn SLO alert (99.5 % of requests without a 5xx over 28 days, health routes excluded) in Azure Monitor, and its runbook `docs/runbooks/slo-fast-burn.md`.
- Octopus project Insights for `workorders`: deployment frequency, lead time, change-failure rate and time to recovery, per environment.
- The P2 exit targets: at least 20 consecutive TDD releases with at least 90 % green (legacy baseline 72 %), and recovery through redeploy-previous in under 15 minutes.

## Offline variant: predict every handoff

Work from `terraform/foundation/`, `terraform/environment/`, `.octopus/workorders-infrastructure/`, `argocd/bootstrap/` and `contracts/platform-contracts.yaml`.

| # | Question | Prediction | Check in |
|---|---|---|---|
| L1 | Which runbooks exist for `infra-prod`, and which one deliberately does not? | | `.octopus/workorders-infrastructure/runbooks/` |
| L2 | Which account does `env-apply` use in `infra-nonprod` today, and in `infra-prod`? | | `.octopus/workorders-infrastructure/variables.ocl` |
| L3 | Why does the environment layer contain no role assignments, and where are the grants it needs? | | ADR-D10; `terraform/foundation/` |
| L4 | Which files does `terraform/environment/bootstrap.tf` read, and which Argo CD Application do they create? | | `argocd/bootstrap/{values,root-app}-nonprod.yaml` |
| L5 | During a nonprod cluster rebuild: what changes about the cluster that forces new federated credentials? What stays the same? | | ADR-D10 consequences; §7.8 |
| L6 | After the rebuild, which secrets must a person put back into `<kv-workorders-platform-nonprod>`, and which come back on their own? | | `docs/bootstrap.md` steps 5–6 |
| L7 | What does `env-destroy` leave behind, and why? | | ADR-D10 guardrails |
| L8 | Which Octopus variable warns before the stored provisioner secret expires, and what retires it for good? | | ADR-C10; R4 |
| L9 | A new Key Vault secret is needed by the app. Which files change, in which package, and who writes the value? | | §7.8; `gitops/workorders/base/secrets.yaml` |

<details>
<summary>Answer key</summary>

- **L1.** `env-plan`, `env-apply` and `rotate-sql-passwords`. There is no `env-destroy` for `infra-prod` (ADR-D10); `provisioner-credential-check` is nonprod-only because the stored secret never reaches prod.
- **L2.** `infra-nonprod`: `azure-runtime-provisioner` during phases 1–2, then `azure-oidc-env-lifecycle-nonprod`. `infra-prod`: `azure-oidc-env-lifecycle-prod` from the first run.
- **L3.** Contributor cannot write `Microsoft.Authorization/*` (E36). Every grant is created in `terraform/foundation` by an Owner, at resource-group scope in advance, directly on the managed identities.
- **L4.** `argocd/bootstrap/values-nonprod.yaml` for the `argo-cd` Helm release and `argocd/bootstrap/root-app-nonprod.yaml` for `argocd-apps`, which creates `platform-root`; that root syncs `argocd/clusters/nonprod` recursively, including the add-ons and `workorders-tdd` and `workorders-uat`.
- **L5.** A new cluster has a new OIDC issuer URL, so the workload federated credentials are re-created by the environment layer (at most 20 per UAMI; create them one at a time, concurrent writes return 409). The UAMIs, their client IDs and their role assignments stay, so the ServiceAccount annotations in `envs/*/config` stay valid.
- **L6.** People put back the Argo CD API token for account `octopus` (`argocd-octopus-gateway-token`), because the new Argo CD instance issues new tokens, and refresh the gateway registration token when the gateway is reinstalled. Values already in the vault (the repo read credential, the app secrets) are synced again by ESO; the workers need a fresh `Octopus.WorkerRegistrationToken`.
- **L7.** The resource groups, the UAMIs, the role assignments and everything in the foundation. The foundation belongs to a human Owner; a runbook must not be able to remove it.
- **L8.** `Provisioner.SecretExpiresOn`, read by `provisioner-credential-check`. Switching `Azure.LifecycleAccount` to OIDC at the phase-2 exit, then deleting the client secret from Entra, Octopus and Codefresh (R4), retires it.
- **L9.** The name goes into design §7.8 and `contracts/platform-contracts.yaml` (pragmatist), the ExternalSecret `workorders-app` in `gitops/workorders/base/secrets.yaml` (gitops-architect), and, if Terraform writes it, `terraform/environment` (sre-security). A security owner writes the value into each `<kv-workorders-{env}>`; `consistency.sh` C11 rejects names that are not in the contracts.

</details>

## Expected Outcome

- A lifecycle map: create, change, rebuild, destroy, with the actor and identity for each.
- A list of what survives a cluster rebuild and what must be redone by a person.
- A short report on the platform's delivery health from Insights and the SLO alert.

## Discussion

1. Why is destroying prod not a runbook, even for an Owner?
2. What would a separate prod subscription (R19) change in the blast radius of `azure-oidc-env-lifecycle-nonprod`?
3. Which metric from Step 5 best shows that the new path is safer than the legacy one?
