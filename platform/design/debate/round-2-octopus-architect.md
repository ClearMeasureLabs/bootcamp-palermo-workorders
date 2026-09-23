# Round 2 — Octopus Architect: critique and convergence

> Role `octopus-architect`. This round applies the user directives that arrived after round 1:
> - credentials are stored in both Octopus and Codefresh;
> - the platform repo is `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh`.
>
> Paths are relative to the platform repo root. The prefix `app:` means the app repo (`ClearMeasureLabs/bootcamp-palermo-workorders`). [UNVERIFIED] marks claims that remain unconfirmed.

## 1. Concessions

- **Helm, not Kustomize** (gitops-architect §6, §8). The ref-source layout is Octopus's own documented pattern (Helm-annotation Example 2): scoping annotations go on the values source and `image-replace-paths.<chart>` goes on the chart. With Helm, Rollout image fields stay inside chart templates. Kustomize needs a Rollout transformer.
- **PR previews without Octopus** (gitops-architect §6; codefresh-engineer D13; pragmatist D13). Previews use the ApplicationSet PR generator only: no ephemeral environments and no release per PR.
- **No tenants; Platform Hub deferred** (pragmatist §3; sre-security §3).
- **Version formula** (codefresh-engineer D6). The version is `MAJOR.MINOR.<first-parent height>`, and release creation uses `IGNORE_EXISTING`.
- **Migrations run in-cluster under a migrator workload identity** (sre-security §6; gitops-architect §6). No Octopus-held DDL password in prod.
- **Provisioning layers** (gitops-architect §3.4; pragmatist §3): foundation/runtime split; no role assignments in the runtime layer (linted); no prod destroy runbook; destroy stays inside resource groups.
- **Read-only prod gateway, "Trigger sync" off** (sre-security §4, TB11); auto-sync plus webhook suffices.
- **SoD guard** (sre-security §8): the deployment creator cannot approve prod.
- **Boundary and consistency lint** (pragmatist §8), plus a separate commit status `platform/tdd` (pragmatist §4).

## 2. Fact-check

| # | Claim (role §) | Finding | Source |
|---|---|---|---|
| F1 | "[UNVERIFIED] Whether Octopus Argo CD steps wait for sync and health" (codefresh §9.2) | Verified. The "Argo CD Application is healthy" option (2026.1+) waits for sync to the *created commit* plus Healthy. It polls every 30 s, and the paused task does not count against the task cap. | octopus.com/docs/argo-cd/steps |
| F2 | "[UNVERIFIED] Wait step can consume the commit SHA of the update step" (gitops §9) | Not needed: the update step's own verification targets its commit. The Wait step also accepts explicit hashes (7–40 hex characters). | …/steps/wait-for-argo-cd-applications |
| F3 | Three-source behavior [UNVERIFIED] (gitops §7.1) | Documented pattern (Examples 2 and 4). Residual risk: every example uses a Git-hosted chart, and a *mapped* Helm-repository/OCI source fails ("Octopus cannot update charts sourced from a Helm repository or OCI feed"). Tags are rewritten in every values file of the mapped source, so `values.yaml` holds no image keys. | …/argo-cd/annotations/helm-annotations; …/argo-cd/troubleshooting |
| F4 | A SQLite preview "costs one pod" (gitops §3.1) and "DbUp `rebuild` (includes TestData)" (pragmatist §3; codefresh D13) | Both previews start with no login data. The Testing filter only runs `EnsureCreated()`. Login lists employees from the database. Employees come only from `ZDataLoader` (IntegrationTests), and `scripts/{Create,Everytime,TestData}` hold only `placeholder.txt`. | app: `src/UI/Server/TestingDatabaseStartupFilter.cs`, `src/UI.Shared/Pages/Login.razor`, `src/IntegrationTests/ZDataLoader.cs` |
| F5 | "readiness /_healthcheck" (sre §8) | Unsafe. `/_healthcheck` and `/health` aggregate the LlmGateway, DataAccess and NeedsReboot checks, and an anonymous `GET /_demo/setneedsreboot/true` flips NeedsReboot. `/alive` runs only the `self` check and is mapped in every environment. | app: `ServiceDefaults/Extensions.cs`, `UI/Server/UIServiceRegistry.cs`, `ServerApplication.cs` |
| F6 | Octopus design deploys "through agent script pods (cluster-admin by default)" (sre §7) | True only for the **agent** (cluster-wide admin unless `scriptPods.serviceAccount.targetNamespaces` and `clusterRole.rules` narrow it; SRE's agent needs both). Round 1 used the **worker**, "limited to modifying its local namespace". | …/kubernetes-agent/permissions; …/workers/kubernetes-worker |
| F7 | Rego fields [UNVERIFIED] (sre §8) | `SkippedSteps`, `Steps[].Slug/Enabled/ActionType` and `Release.GitRef` all exist. `Release` is absent for runbook runs, and `GitRef` exists only for version-controlled projects, so scope the policy to deployments. | …/platform-hub/policies/schema |
| F8 | `obtain-oidc-id-token` without `AUDIENCE` (sre §8) | Wrong audience. The default is `https://g.codefresh.io`, but Octopus requires `aud` = the service-account ID. | codefresh.io/steps/step/obtain-oidc-id-token; octopus.com/docs/api/authentication/openid-connect |
| F9 | `steps.…output.id_token` and `…output.access_token` (pragmatist §5) | The documented names are `ID_TOKEN` and the exported variable `OCTOPUS_ACCESS_TOKEN`. Answer to Q3: `create-release` accepts `OCTOPUS_ACCESS_TOKEN` and `GIT_COMMIT`. | …/octopusdeploy-login; …/octopusdeploy-create-release |
| F10 | Platform Hub is system-team only (pragmatist §3); Kustomize needs a Rollout transformer (gitops §6) | Both confirmed. | octopus.com/docs/platform-hub; argoproj.github.io/argo-rollouts/features/kustomize/ |

**Own round-1 claims retracted:**
- "create-release documents `GIT_REF` but no commit SHA." It documents `GIT_COMMIT`, `PACKAGE_VERSION` and `IGNORE_EXISTING`.
- Rego `"action": "block"` marked [UNVERIFIED]. It is verified.
- `run-acceptance-tests` scoped to tdd and uat. This was **dangerous**: `ZDataLoader.LoadData()` starts with `DatabaseEmptier.DeleteAllData()`, so it would wipe UAT data. It is now scoped to TDD and previews only.
- Withdrawn:
  - Kustomize (D3);
  - cohort tenants (D7);
  - Octopus ephemeral previews (D14);
  - CaC in the app repo;
  - DbUp as a password-bearing worker script.

## 3. Contested decisions

| # | Final stance | Argument | Strongest counter rejected |
|---|---|---|---|
| C1 | **Change:** Helm chart (OCI, versioned independently, pinned per env in `env.yaml` by PR) plus per-env values in Git. Octopus writes only `gitops/envs/<env>/release.yaml`, via the image-tag step with ref-source scoping and `image-replace-paths.chart`. | A documented Octopus pattern; Octopus never touches Rollout fields. | Kustomize (pragmatist; round-1 self): needs a Rollout transformer and a second preview model. The OCI risk (F3) gets a TDD spike; fallback is a Git-hosted chart path. |
| C2 | **Keep** migrate-before-pin; **change** the mechanism (§5.1). Previews use a SQL Server container, DbUp `update` and a seed Job. | A failed migration never produces a pin, logs sit beside the approval, and the approach is secretless. | Sync hook (gitops). Hooks run on every sync operation, are skipped by selective sync, and let a pin land whose schema never applied. |
| C3 | **Change** to phased rollout (§5.2): rolling update, then blue-green; canary deferred. | Octopus's "healthy" check works for all three strategies. Blue-green needs no router and works with one replica. | Canary with SLO analysis (sre). A low-traffic app gives the analysis no signal, and the in-memory WebSocket hub and Blazor WASM assets dislike mixed versions. |
| C4 | **Change** to the PR generator only: maintainer-applied label, phase 3+, SQL container, DbUp, seed Job. | One preview model, with cleanup on PR close. | Octopus ephemeral environments (round-1 self): gateway scoping for them is [UNVERIFIED], and they get no lifecycles, Insights or tenants. SQLite (gitops): no login data (F4). |
| C5 | **Change** to one platform repo, per the directive. Writers per path are listed in §6; Argo CD reads with its own read-only credential. **Recommendation to the user:** make the repo private. | The directive decides the repo. The stored PAT already covers the org. | A separate releases repo (gitops). With an org-wide PAT the split buys nothing. Use a CI path check plus drift detection instead. |
| C6 | During the parallel run, `build-result` stays the only required check, `codefresh/ci` is informational, and Codefresh `workorders/release` is the build of record for the new space. After phase 4 **and** 50 agreeing builds, `codefresh/ci` becomes required; ARM and Windows LocalDB stay on GitHub Actions under a trimmed, still-required `build-result`. | Evidence before the flip; a single release creator per space. | Flip on 50 agreeing builds alone (codefresh D14): legacy `deploy.yml` still consumes GitHub Actions artifacts until cutover, so those gates must stay authoritative. |
| C7 | **Keep** Codefresh as the sole authority minting `2.5.<first-parent height>`. Image tag = package version = release number. Hotfix releases are `…-hotfix.<n>`, built over existing packages. Legacy stays on `2.4.<run>` until it retires. | Deterministic per commit, which ends the EP20/EP21 collisions; 2.5 sorts after 2.4. | `3.0.<height>` (pragmatist). A major bump signals a breaking change to API/MCP consumers that did not happen. |
| C8 | **Change:** cut tenants. | Single customer; a licensed dimension; per-student apps belong in the sample-apps org. | Cohort sandboxes (round-1 self): labs can run read-only against TDD and UAT. |
| C9 | **Change:** defer Platform Hub and keep the Rego files ready. Interim: CODEOWNERS, protected `main`, manual interventions and the SoD step. | It needs system-team permissions (the Space Manager account lacks them) and an Enterprise license. | Enforce now (sre, gitops): no identity can apply it today. |
| C10 | **Recommendations to the user** (§5.3). | One user, one scope, a dated exit. | Delete the secret now (sre): that conflicts with the user's storage decision, so it becomes the recommended end state. |
| C11 | Octopus TDD process: after the healthy check and smoke test, run `ChurchBulletin.AcceptanceTests` on the Kubernetes worker in Codefresh's CI image. A failure fails the deployment, so the lifecycle blocks UAT. TRX files attach as artifacts; status `platform/tdd`. Never run in UAT or Prod. | One orchestrator; results attach to the release. | Codefresh waiting on the deploy and then testing: CI would idle on deployments. |
| C12 | Prerequisites below; only the probe change blocks phase 2. | Keeps `src/**` changes off the critical path. | App changes as optional follow-ups (gitops §9 Q3; sre §9 Q5): F4 and F5 make probes and seeding prerequisites. |

**C12 prerequisites:**

| Change | Owner | Needed by |
|---|---|---|
| Probes on `/alive`; a DB-only `/ready` added later | platform (chart); app team | phase 2 / phase 3 |
| Block `/_demo/*` routes in uat and prod; gate them in code later | platform; app team | phase 3 |
| Worker image (`app: platform/docker/worker.Dockerfile`) with `RemotableBus__ApiUrl`; no HTTP health, so an exec probe | codefresh-engineer | phase 3 |
| Migrator image; DbUp Workload Identity auth option | app team | phase 4 (nonprod interim: vaulted password) |
| Seed entry point outside the test project | app team | phase 3 (previews) |
| App SQL auth `Active Directory Workload Identity`; connection string must start with `Server=` (`ShouldUseLearningTransport` matches `Data Source=`) | platform config + SRE | phase 3 |
| NServiceBus installers moved out of runtime | app team | phase 4 |

## 4. Non-negotiables

1. **One pin writer.** Only Octopus writes `gitops/envs/{tdd,uat,prod}/release.yaml`. Break-glass commits are drift-detected and followed by a redeploy. No Image Updater, and no CI commits or CI syncs against named environments.
2. **Migration before pin.** For every named environment, the release's migration succeeds before its pin commit. A failure produces no commit.
3. **One release identity.** The version is minted once per commit and used verbatim as image tag, package version and release number. Only the Codefresh master pipeline creates releases, over OIDC.

## 5. Proposed compromises

**5.1 C2 — Octopus orchestrates, Kubernetes executes, Argo CD applies desired state.**

The Octopus step runs on the in-cluster Kubernetes worker. It creates the Job `db-migrate-<version>` in the worker's own namespace `octopus-worker`, which is the only namespace a worker may modify. The Job:
- runs image `workorders/migrator:<version>`;
- uses service account `workorders-migrator-<env>`, federated to that environment's migrator identity;
- has `backoffLimit: 0` and `activeDeadlineSeconds: 900`.

The step waits, streams Job logs into the task log, then commits the pin; prod first records a PITR marker. The Job is an execution record, not desired state. GitOps gets in-cluster execution, a private endpoint, Workload Identity and no agent target; Octopus gets ordering, logs beside the approval, and no commit on failure.

**5.2 C3 — phased rollout.**

| Phase | Strategy |
|---|---|
| Phase 2 | `RollingUpdate` with `maxUnavailable: 0`, readiness on `/alive`, and the Octopus smoke gate (Degraded fails in TDD, #9017). |
| Phase 3 | UI.Server becomes a blue-green Rollout with pre-promotion smoke and `autoPromotionEnabled: true`, because a Suspended Rollout never reports Healthy. The Worker stays `RollingUpdate`. |
| Later | Canary, once prod traffic and Managed Prometheus exist. SRE's burn-rate alerts run from day one. |

In every phase, Octopus verification uses a 900 s timeout.

**5.3 C10 — provisioning credential (recommendations to the user, not decisions).**
- **Octopus:** narrow `Azure Runtime Provisioner` (today unrestricted) to `infra-nonprod` and `infra-prod`. Include the `Azure Runtime Provisioning` library variable set only in `workorders-infrastructure`. Steps should use the account, not the raw `AZURE_CLIENT_SECRET`. Use plan → manual intervention → apply. An Owner places `CanNotDelete` locks on the prod and legacy resource groups.
- **Codefresh:** leave `azure-runtime-provisioner` attached to no pipeline (codefresh D16).
- **OIDC:** in phase 1, an Entra app owner adds federated credentials for issuer `<OCTOPUS_URL>`, one subject per infra environment (no wildcards). In phase 3 the account moves to Azure OIDC. Deleting the secret from both stores is the user's decision; sre's end state.

## 6. Naming and interface contracts

**Repositories and writers.**

| Path | Writer | Reader |
|---|---|---|
| `gitops/envs/{tdd,uat,prod}/release.yaml` | Octopus only, on `main` (Git credential `GitHub clearmeasure-aisf-sample-apps`) | Argo CD |
| `gitops/envs/<env>/{env.yaml,values.yaml}`, `gitops/previews/values.yaml` | Humans, via PR | Argo CD |
| `argocd/bootstrap/`, `argocd/clusters/{nonprod,prod}/` | Humans, via PR | Argo CD |
| `.octopus/workorders/`, `.octopus/workorders-infrastructure/` | PR; Octopus UI edits go to branches | Octopus |
| `octopus/terraform/`, `octopus/platform-hub/policies/`, `terraform/{foundation,environment}/`, `design/` | Humans, via PR | Octopus runbooks / people |
| `app: platform/codefresh/`, `app: platform/docker/`, `app: platform/chart/workorders/` | Developers, via PR | Codefresh |

**Octopus.**

| Object | Names |
|---|---|
| Projects | `workorders`, `workorders-infrastructure`; project group `Work Orders` |
| Environments | `tdd`, `uat`, `prod`, `infra-nonprod`, `infra-prod` |
| Lifecycles | `workorders-standard` (tdd auto → uat → prod); `workorders-hotfix` (uat → prod) |
| Channels | `Default` (Git ref `refs/heads/main`); `Hotfix` |
| Deployment steps | `prod-go-no-go`, `sod-guard`, `migrate-database`, `update-argo-cd-image-tags`, `smoke-test`, `acceptance-tests` (tdd only), `report-commit-status` |
| Runbooks | `db-backup`, `db-restore-pitr`, `rotate-sql-credential`, `run-acceptance-tests` (tdd only); `env-plan`, `env-apply`, `env-destroy` (nonprod only), `provisioner-credential-check` |
| Pools and accounts | Worker pools `aks-nonprod`, `aks-prod`. OIDC accounts `azure-deploy-nonprod`, `azure-deploy-prod`. Service account `svc-codefresh-release`. Teams `platform-engineers`, `workorders-developers`, `uat-signoff`, `prod-approvers`. |
| Stored (reuse) | Account `Azure Runtime Provisioner`; variable sets `Azure Runtime Provisioning` and `GitHub AISF Sample Apps` (neither included in `workorders`); Git credential `GitHub clearmeasure-aisf-sample-apps` |
| New variable set `WorkOrders Environment` | `App.BaseUrl`, `Sql.ServerFqdn`, `Sql.Database`, `KeyVault.Name`, `WorkerPool`, `Smoke.FailOnDegraded`, `Migrator.ServiceAccount` |

**Argo CD** (names follow gitops-architect):
- Instances `argocd-nonprod` and `argocd-prod`, each with a gateway release `octopus-argocd-gateway`.
- AppProjects `platform`, `workorders`, `workorders-preview`.
- ApplicationSets `workorders-envs` and `workorders-previews`.
- Applications `workorders-{tdd,uat,prod}` and `workorders-pr-<n>`.
- Sources are named `chart` (OCI) and `env` (platform repo, ref). Value files: `$env/gitops/envs/<env>/values.yaml`, then `$env/gitops/envs/<env>/release.yaml`.
- Annotations: `argo.octopus.com/project.env: workorders`, `argo.octopus.com/environment.env: <env>`, and `argo.octopus.com/image-replace-paths.chart` built from `uiServer` and `worker` `image.repository:image.tag`.

**Artifacts** (under `<acr-name>.azurecr.io`):
- Release images: `workorders/ui-server:<version>`, `workorders/worker:<version>`, `workorders/migrator:<version>`.
- Preview images: `previews/ui-server:sha-<40-hex>`.
- Chart: `oci://…/helm/workorders:<chart-version>`.
- Octopus package IDs and `PACKAGE_IDS` are identical to the release image repositories above, plus `ChurchBulletin.AcceptanceTests`.

**Handoff arguments:**
- `obtain-oidc-id-token` sets `AUDIENCE` to the `svc-codefresh-release` ID.
- `octopusdeploy-create-release` takes `PROJECT: workorders`, `CHANNEL: Default`, `RELEASE_NUMBER` and `PACKAGE_VERSION` equal to the version, `GIT_REF: refs/heads/main`, and `IGNORE_EXISTING: true`.
- **No `GIT_COMMIT`:** the CaC lives in the platform repo, so an app commit SHA would not resolve there. App traceability comes from build information.

**Secret names only:**
- Key Vault: `workorders-sql-connection`, `workorders-openai-key`, `workorders-api-validation-key`, `workorders-sql-migrator-password` (interim), `workorders-sql-test-password` (TDD).
- Codefresh contexts: `workorders-ci`, `workorders-release`, plus the stored `azure-runtime-provisioner` (unattached) and `github-aisf-sample-apps-token`.

**Open item:** no stored credential can post `platform/tdd` statuses on the ClearMeasureLabs repo. This needs a user decision: a GitHub App or a statuses-scoped token.
