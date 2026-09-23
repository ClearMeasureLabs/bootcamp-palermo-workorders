# Round 2 Critique: GitOps Architect

| Field | Value |
|---|---|
| Role | `gitops-architect` |
| Round | 2: cross-critique |
| Date | 2026-09-23 |
| Conventions | Design only. Unless prefixed `app:`, paths are relative to the environment repo `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh`. Statements about stored credentials are **recommendations to the user**, not decisions. |

## 1. Concessions

- **Kustomize, not Helm** (octopus-architect §6 D3; pragmatist §6 D8).
  - For Helm sources, the Octopus image-tag step replaces tags "in every Helm values file referenced by the Application — both the chart's default `values.yaml` and any files listed in the Application's `spec.source.helm.valueFiles`".
  - For Kustomize sources, it edits only `newTag`: "No other files will be edited" ([Octopus](https://octopus.com/docs/argo-cd/steps/update-application-image-tags)).
  - Two workloads plus a Job do not need a chart.
- **Octopus runs migrations before the tag commit** (octopus-architect D4; codefresh-engineer §7.2; sre-security §6; pragmatist D6).
  - The migration log sits beside the approval.
  - A failed migration never produces a commit.
  - Argo CD hooks "are **not** run" during selective sync ([Argo CD](https://argo-cd.readthedocs.io/en/stable/user-guide/selective_sync/)).
- **Rolling update first** (pragmatist D14). With one replica and no metrics pipeline, the database is the real risk.
- **The app blocks canary** (octopus-architect §7 risk table). Verified: `RealtimeNotificationHub` keeps sockets in an in-process `ConcurrentDictionary<Guid, WebSocket>`, so splitting traffic splits notification delivery.
- **SQL Server connection strings must start with `Server=`** (sre-security R6). `DataContext` and `ShouldUseLearningTransport` both switch to SQLite on any `Data Source=` string.
- **CI mints the version; Octopus takes it verbatim with `IGNORE_EXISTING`** (codefresh-engineer D6/D7; octopus-architect D12).
- **Codefresh pushes to ACR with runner Workload Identity, not Codefresh OIDC** (sre-security §6; codefresh-engineer §7.1). The Codefresh token `sub` embeds `scm_user_name`.
- **Named environments use plain Applications; only previews use an ApplicationSet** (octopus-architect §3; pragmatist §5).
- **Boundary lint and consistency checks** (pragmatist §8) enforce the single-writer rules in CI.
- **AppProjects `platform-addons`, `workorders-nonprod`, `workorders-prod`, `workorders-previews`, with sync impersonation** (sre-security §3).

## 2. Fact-check

| Claim in another paper | Corrected fact | Source |
|---|---|---|
| sre-security §8: "readiness /_healthcheck". | `/_healthcheck` runs every registered check. That includes `LlmGateway`, a live chat call that returns Unhealthy on an exception, and `NeedsReboot`, which an anonymous GET to `/_demo/setneedsreboot/true` flips. Ingress denies do not stop in-cluster callers. An Azure OpenAI outage would take every pod out of service. Use `/alive` until a database-only `/ready` endpoint exists. | `app:src/UI/Server/UIServiceRegistry.cs` L64–71; `app:src/LlmGateway/CanConnectToLlmServerHealthCheck.cs`; `app:src/UI/Server/ServerApplication.cs` |
| sre-security §8: Kyverno `mutateDigest: true` on Deployments and Jobs. | Kyverno's rewrite makes the live object differ from Git, and self-heal keeps reverting it. Server-Side Diff "does not include changes made by mutation webhooks by default"; it needs `ServerSideDiff=true,IncludeMutationWebhook=true`, and Legacy diff is the default. Separately, `verifyDigest: true` rejects tag-only manifests, and Octopus writes tags. | [Argo CD diff strategies](https://argo-cd.readthedocs.io/en/stable/user-guide/diff-strategies/); [Kyverno mutate](https://kyverno.io/docs/policy-types/cluster-policy/mutate/) |
| octopus-architect D3: the Helm mode rewrites values files shared across environments. | Correct. Plain-YAML mode is worse for Rollouts: "CRDs are not included". | [Image-tag step](https://octopus.com/docs/argo-cd/steps/update-application-image-tags) |
| octopus-architect §8: the gateway needs "applications get,sync". | `applications, sync` is optional and needed only to trigger syncs. A read-only token with Trigger sync off (sre-security handoff 11) works. | [Argo user](https://octopus.com/docs/argo-cd/instances/argo-user) |
| pragmatist §7: "Rollouts analysis needs traffic and a provider the app lacks". | Partly wrong. The Job provider passes when the Job "completes and had an exit code of zero", with no metrics store or traffic. Only post-promotion error-rate analysis needs traffic. | [Job provider](https://argoproj.github.io/argo-rollouts/analysis/job/) |
| codefresh-engineer D10: an optional GitOps Runtime in existing-Argo CD mode. | Requires a "valid, non-expiring Argo CD Admin API token" plus a Shared Configuration Repository: a standing admin credential in exchange for a dashboard. Also, "The GitOps Cloud product is no longer available". | [Codefresh BYOA](https://codefresh.io/docs/docs/installation/gitops/runtime-install-with-existing-argo-cd/); [Octopus](https://octopus.com/codefresh) |
| octopus-architect §3/§8: the new path reuses image `churchbulletin.ui`. | Legacy `build.yml` pushes `churchbulletin.ui:latest` from every branch, so that repository can never be tag-locked. The new path needs its own repositories. | `app:.github/workflows/build.yml` |

**Own round-1 claims (gitops-architect), retracted or resolved:**

- *"Kustomize needs a CRD transformer to change Rollout images."* **Wrong.** Kustomize's default image rules (`spec/template/spec/containers[]/image`) are not limited by resource kind. Argo Rollouts' `rollout-transform.yaml` adds only `nameReference`, `commonLabels`, `commonAnnotations`, `varReference` and `replicas` ([kustomize](https://github.com/kubernetes-sigs/kustomize/blob/master/api/internal/konfig/builtinpluginconsts/images.go); [Rollouts](https://argoproj.github.io/argo-rollouts/features/kustomize/)).
- *"GitHub has no path-scoped push permission."* **Wrong.** Push rulesets can "Restrict file paths". Bypass can go to "roles, specific teams, or GitHub Apps", not to individual users ([GitHub](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets)). Which GitHub plans offer push rulesets is [UNVERIFIED].
- *"With a separate releases repo, Octopus's credential can change which version runs, never how it runs."* **Void.** The stored Git credential is restricted to `https://github.com/clearmeasure-aisf-sample-apps/*`, which is every repo in the org.
- *Preview image tag `pr-<n>-<sha8>`.* **Wrong.** It does not match Codefresh's 7-character `CF_SHORT_REVISION`. Replaced by the full `head_sha`.
- **Resolved:**
  - The gateway's sync permission is optional (see the table above).
  - EF Core 10 depends on `Microsoft.Data.SqlClient >= 6.1.1` ([NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.0)). That version supports `Active Directory Workload Identity` with no new package.

## 3. Contested decisions

| # | Final stance | Argument | Strongest counter-argument rejected, and why |
|---|---|---|---|
| C1 Packaging | **Change to Kustomize** base plus overlays. Octopus edits only `newTag` in `gitops/workorders/envs/<env>/kustomization.yaml`. Humans own `envs/<env>/config/`. | Octopus writes one documented field and never touches other values files. Kustomize's image rules reach Rollout containers natively. Previews override the image through `spec.source.kustomize.images`. | *"Helm gives a versioned, promotable chart"* (gitops-architect, round 1). Base changes need PR review in either format, and the Helm mode rewrites shared files. |
| C2 Migrations | **Change to Octopus** for `tdd`, `uat` and `prod`, in this order: back up (prod only), run DbUp `update`, commit the tag, wait. The migration runs in-cluster on a Kubernetes worker pool per environment. Each pool's service account federates only to that environment's migrator identity and never writes Argo-managed objects. Previews: SQLite needs no migration; SQL-backed previews use a sync-wave `rebuild` Job. | A failed migration produces no commit. It runs once per release, not on every sync. The log sits with the approval. | *An Octopus agent creating the Job in the app namespace* (sre-security §4). That puts a second writer into Argo-managed namespaces. Per-environment pools give the same identity isolation. |
| C3 Progressive delivery | **Rolling update** in phases 1–3. At Prod cutover, a **blue-green Rollout**: a Job-provider smoke test before promotion, and error-rate analysis after it in `dryRun` until the metrics are proven. Canary waits for a backplane in the notification hub. | Blue-green moves every WebSocket connection at once. The smoke test needs no traffic. | *Canary with SLO analysis* (sre-security §1). There is one replica, and the in-process hub would split notifications between versions. |
| C4 Previews | **ApplicationSet PR generator only**, with label `preview`, once TDD on AKS is stable. SQLite, UI only, by default; a second label adds SQL. Image tags use the full SHA. | Created and pruned with the PR, natively and cheaply. | *Adding Octopus ephemeral environments* (octopus-architect D14). They cannot join lifecycles (that paper's own sources say so), and gateway scoping to `pr-<n>` is [UNVERIFIED] there too. It would be a second preview model. |
| C5 Repo topology | **Change to one environment repo**, `basic-environment-octopus-codefresh`, per the user directive. Recommend private visibility. Octopus writes only `gitops/workorders/envs/*/kustomization.yaml`, enforced by a push ruleset whose bypass is a bot team or GitHub App. Everything else goes through PR plus CODEOWNERS. Argo CD only reads. Codefresh never writes. | One home, as directed. A ruleset enforces the path limits that the credential cannot. | *A separate releases repo* (gitops-architect, round 1). The stored credential covers the whole org, so the split buys nothing. Recommend the user narrow the credential to this repo. |
| C6 CI and required check | During the parallel run, GitHub Actions `build-result` stays the required check and `codefresh/ci` is informational. Flip after 50 consecutive builds where both agree (codefresh-engineer D14). ARM and Windows LocalDB stay on GitHub Actions as a second required check, which needs an approved `.github/**` change. Only Codefresh `workorders/release` publishes artifacts for the new platform. | One producer of release artifacts. Gate parity is proven, not assumed. | *Move every gate to Codefresh now*. Codefresh Windows builds are incubation-only, and ARM needs Enterprise (codefresh-engineer §8.1). |
| C7 Versions | Codefresh mints `2.5.<first-parent height>`. Release number, package version and image tag are all that value. Legacy keeps `2.4.<run>` in its own image repositories. | Deterministic per commit, so reruns are safe. `2.5.x` always sorts above `2.4.x`. | *`3.0.<count>`* (pragmatist). It is equally collision-free, but the app has no breaking change. The CI owner decides. |
| C8 Tenants | **Cut.** Keep the `argo.octopus.com/tenant` annotation available for a later cohort ApplicationSet. | The brief states no cohort requirement. Every tenant multiplies pins and commits. | *A tenant per cohort* (octopus-architect D7). Premature, and tenants are licensed. |
| C9 Platform Hub | **Cut from the critical path.** Revisit once an Enterprise licence and system-team rights are confirmed. | GitOps guardrails (AppProjects, rulesets, Kyverno) do not depend on it. | *A Rego go/no-go policy* (octopus-architect; sre-security). For phase 1, a manual intervention plus RBAC does the same job. |
| C10 Provisioning credential | Recommendations to the user: (1) use it only from `workorders-infrastructure` runbooks; (2) restrict the account, currently unrestricted, to `infra-*` environments; (3) keep the Codefresh context unattached, then delete it; (4) add an Octopus federated credential and switch to OIDC by phase 3; (5) then delete the secret. It never reaches Argo CD, a cluster or a branch pipeline. | One holder, audited, with an exit path. | *Delete immediately after bootstrap* (sre-security §6). Its replacement needs role assignments first, so the secret has to bridge that gap. |
| C11 TDD acceptance tests | Octopus runs them in TDD: wait for Synced and Healthy at the commit, check that `/_version` matches the release, then run Playwright on the nonprod Kubernetes worker using the Codefresh CI image. TRX results are saved as Octopus artifacts. A failure fails the deployment, and the lifecycle then blocks UAT. | The results stay on the release record. | *Playwright as an Argo PostSync hook or analysis Job*. The results would leave the release record, and a hook failure would not fail the deployment. |
| C12 App prerequisites | **For TDD on AKS, configuration only (platform-owned):** `/alive` probes; `/_demo/*` and `/_healthcheck/detailed` denied at the Gateway; SQL strings starting with `Server=`; a Worker image built without changes to `src`; `RemotableBus__ApiUrl` pointing at the in-cluster Service. **Before Prod cutover (app team, via work items):** a `/ready` endpoint; Entra authentication in DbUp; a Worker health endpoint; NServiceBus installers moved out of the runtime; a hub backplane before running more than one replica. | Unblocks phase 2 without touching `src`. | *Readiness on `/_healthcheck`* (sre-security). Refuted in section 2. |

## 4. Non-negotiables

1. **Argo CD is the only thing that applies Argo-managed objects.**
   - Octopus and Codefresh run no `kubectl`, Helm or Kubernetes-YAML steps against app namespaces. The only exception is the one-time bootstrap install.
   - An admission controller may mutate those objects only if the mutation shows up in Argo CD's diff (`ServerSideDiff=true,IncludeMutationWebhook=true`). Otherwise it must not mutate them.
2. **Each GitOps path has one writer.**
   - Octopus writes only the image `newTag` values.
   - No Image Updater, no CI commits, no Codefresh promotions.
   - Rulesets, CODEOWNERS and the boundary lint enforce this.
3. **Auto-sync, prune and self-heal in every environment, Prod included.** Rollback means Octopus "redeploy previous release", never an Argo CD UI rollback or `kubectl`.

## 5. Proposed compromise for the top three conflicts

- **Rollout strategy (sre-security vs pragmatist vs gitops).**
  - Phases 1–3 use RollingUpdate with `maxUnavailable: 0`.
  - At cutover, Prod moves to blue-green. The smoke test runs through the Job provider. Post-promotion SLO analysis uses sre-security's Workload Identity probe against Azure Monitor. It runs in `dryRun` first and becomes blocking after 14 days of clean data.
  - Canary becomes eligible once the notification hub has a backplane.
- **Admission vs reconciliation (sre-security vs gitops).**
  - Verify signatures at the Deployment/Rollout/Job level.
  - Allow `mutateDigest` only on Applications that set `ServerSideDiff=true,IncludeMutationWebhook=true`. Elsewhere, verify without mutating and rely on ACR tag locks.
  - Run both variants in Audit mode in nonprod before switching to Enforce.
- **Repo and credential (user directive vs the round-1 two-repo split).**
  - One environment repo, with path rulesets.
  - Recommend the user replace org-wide write access with a GitHub App installed only on this repo.
  - Argo CD gets its own read-only App credential.

## 6. Naming and interface contracts

```text
app:ClearMeasureLabs/bootcamp-palermo-workorders (public)
  platform/codefresh/                pipelines workorders/ci, workorders/release
  platform/containers/               worker.Dockerfile, db-migrator.Dockerfile (root Dockerfile untouched)
clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh (recommend private)
  argocd/bootstrap/                  argo-cd Helm values; root Application per cluster
  argocd/clusters/{nonprod,prod}/    projects/, addons/, apps/workorders-<env>.yaml, appsets/workorders-previews.yaml (nonprod)
  gitops/workorders/base/            ui-server, worker, Services, HTTPRoute, SecretStore, ExternalSecrets
  gitops/workorders/envs/<env>/kustomization.yaml    Octopus-only: resources [config], images[].newTag
  gitops/workorders/envs/<env>/config/               humans via PR: namespace, patches, ConfigMaps
  gitops/workorders/components/bluegreen/            Rollout patch, enabled in prod at cutover
  gitops/workorders/previews/        overlay for the PR generator
  octopus/terraform/  octopus/projects/{workorders,workorders-infrastructure}/
  terraform/{foundation,environment}/
  design/                            design record
```

| Contract | Value |
|---|---|
| Images (ACR) | `<acr-name>.azurecr.io/workorders/{ui-server,worker,db-migrator}:<version>`; previews `previews/ui-server:pr-<n>-<sha40>` |
| Version | `2.5.<first-parent height>` = Octopus release = package version = image tag |
| Octopus packages | Docker feed `acr`, with package IDs equal to the three repositories; built-in feed: `ChurchBulletin.Database`, `ChurchBulletin.AcceptanceTests` |
| Octopus objects (octopus-architect names) | Projects `workorders`, `workorders-infrastructure`. Environments `tdd`, `uat`, `prod`, `infra-nonprod`, `infra-prod`. Lifecycles `mainline`, `hotfix`. Channels `Mainline`, `Hotfix`. Worker pools `aks-tdd`, `aks-uat`, `aks-prod`. Runbooks `env-create`, `env-destroy`, `db-backup`, `db-restore-pitr`, `run-acceptance-tests`. Argo CD instances `argocd-nonprod`, `argocd-prod`. Variables `App.BaseUrl`, `Azure.KeyVault`, `Smoke.FailOnDegraded`, `WorkerPool`. |
| Existing stores (names only) | Octopus: `Azure Runtime Provisioner` account, `Azure Runtime Provisioning` variable set, Git credential `GitHub clearmeasure-aisf-sample-apps`, variable set `GitHub AISF Sample Apps`. Codefresh: context `azure-runtime-provisioner`, Git integration `github-aisf-sample-apps`, context `github-aisf-sample-apps-token`. |
| Argo CD | AppProjects `platform-addons`, `workorders-nonprod`, `workorders-prod`, `workorders-previews`. Applications `root`, `workorders-{tdd,uat,prod}`, `workorders-pr-<n>`. ApplicationSet `workorders-previews`. Annotations `argo.octopus.com/project: workorders` and `argo.octopus.com/environment: <env>`. |
| Kubernetes | Namespaces `workorders-<env>`, `workorders-pr-<n>`. Workloads and ServiceAccounts `ui-server`, `worker`, `db-migrator`. Services `ui-server`, `ui-server-preview`. SecretStore `key-vault`. |
| App config keys | `ConnectionStrings__SqlConnectionString` (must start with `Server=`), `ApplicationInsights__ConnectionString`, `RemotableBus__ApiUrl=http://ui-server.<ns>.svc.cluster.local/api/blazor-wasm-single-api` (path from `app:src/AcceptanceTests/ServerFixture.cs`), `AI_OpenAI_Url`, `AI_OpenAI_Model`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` |
| Key Vault secret names | `sql-connection-string`, `ai-openai-apikey`, `api-validation-key`, `migrator-sql-password` (interim, until DbUp supports Entra) |
| Codefresh | Pipelines `workorders/ci`, `workorders/release`. Contexts `workorders-ci`, `workorders-release`. Commit statuses `codefresh/ci`, `codefresh/release`. |
