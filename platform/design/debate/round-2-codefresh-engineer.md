# Round 2: Codefresh Engineer Critique and Rebuttal

| Field | Value |
|---|---|
| Role | `codefresh-engineer` |
| Round | 2 (cross-critique) |
| Date | 2026-09-23 |
| Inputs | Four round-1 papers, updated brief, user directives (credential stores, sample-apps org, env repo `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh`) |

## 1. Concessions

- **Split runner runtimes** (sre-security §3, §4 handoffs 1 and 4). PR pipelines run on a runtime with no cloud identity; only the master pipeline gets push and Octopus rights. Previews move to their own label-triggered pipeline.
- **Provenance attestation** (sre-security §8). The release pipeline adds a SLSA provenance attestation beside the SBOM, honestly labelled step-authored (L2 at most), so admission can require it.
- **Kustomize `newTag` as the Octopus write contract** (octopus-architect §6 D3; pragmatist D8). Verified below; this replaces round-1 neutrality.
- **Image names `churchbulletin.*`** (octopus-architect §8; sre-security §8 admission glob). This keeps continuity with legacy names and the AI Software Factory. Round-1 `workorders/ui-server` is dropped.
- **Full commit SHA in preview tags** (pragmatist §7). `CF_SHORT_REVISION` is 7 characters and the generator's `head_short_sha` is 8.
- **Commit status `platform/tdd`**, distinct from legacy "Deploy to TDD" (pragmatist §4); **equal TRX totals** as a shadow-mode criterion (pragmatist §8 P1); **boundary lint** (pragmatist §8).
- **`/alive` for probes, never `/_healthcheck`** (gitops-architect §7.1). Code confirms it (§2).
- **Foundation layer for role assignments** (octopus-architect §3; gitops-architect §3.4; sre-security L1). Codefresh needs none of it.
- **Worker as a plain Deployment, never a Rollout** (gitops-architect §6): competing consumers cannot split traffic.

## 2. Fact-check

### 2.1 Claims in other papers

| # | Claim | Corrected fact | Source |
|---|---|---|---|
| F1 | "Create-release documents `GIT_REF`, no commit SHA" (octopus-architect §7) | The Codefresh step documents `GIT_COMMIT` ("Git Commit Hash; Specify this in addition to Git Reference") and `IGNORE_EXISTING`. With CaC in the env repo, the commit is the env repo's, never the app's (§6.7). | https://codefresh.io/steps/step/octopusdeploy-create-release |
| F2 | Create-release sets custom field `PullRequestNumber` (octopus-architect §4 step 2) | None of the step's documented arguments takes custom fields, and none of the eight Codefresh Octopus steps deprovisions an ephemeral environment. Octopus's own demo uses GitHub Actions `OctopusDeploy/deprovision-ephemeral-environment@v1.0.0` on `pull_request: closed`. | https://octopus.com/docs/packaging-applications/build-servers/codefresh-pipelines; https://github.com/OctopusDeploy/ephemeral-environments-demo |
| F3 | `ID_TOKEN: "${{steps.octopus_id_token.output.id_token}}"`, `OCTOPUS_ACCESS_TOKEN: "${{steps.octopus_login.output.access_token}}"` (pragmatist §5) | Documented forms: `ID_TOKEN` (step output `ID_TOKEN`, uppercase) and the exported `${{OCTOPUS_ACCESS_TOKEN}}`. Pragmatist Q3 answered: create-release and push-build-information both accept `OCTOPUS_ACCESS_TOKEN`; create-release accepts `GIT_COMMIT`. | https://codefresh.io/steps/step/obtain-oidc-id-token; https://codefresh.io/steps/step/octopusdeploy-login |
| F4 | "Codefresh gets only AcrPush via OIDC" (pragmatist §3) | Entra needs an exact `sub`. Codefresh push-triggered subjects embed `scm_user_name`, and flexible federated credentials (preview) support only GitHub, GitLab and Terraform Cloud issuers. Use a repository-scoped ACR token; runner workload identity stays [UNVERIFIED] (sre-security). | https://codefresh.io/docs/docs/integrations/oidc-pipelines/; https://learn.microsoft.com/en-us/entra/workload-id/workload-identities-flexible-federated-identity-credentials |
| F5 | SQLite preview: "a PR namespace costs one pod" (gitops-architect §3.1) | Schema only: `TestingDatabaseStartupFilter` runs `EnsureCreated` under `ASPNETCORE_ENVIRONMENT=Testing`. There is no seed data: `ZDataLoader` is acceptance-test code, and `Login.razor.cs` queries employees. Previews need SQL Server with DbUp `rebuild` (TestData) or a seed Job. | `src/UI/Server/TestingDatabaseStartupFilter.cs`, `src/UI.Shared/Pages/Login.razor.cs` |
| F6 | Helm OCI chart plus `$config` and `$release` values files, written by Octopus (gitops-architect §6, §8) | The step rewrites "every Helm values file referenced by the Application — both the chart's default `values.yaml` and any files listed in … `valueFiles`", and never `valuesObject`. The config-repo values file is referenced too, so the two-repo credential split fails. OCI-chart default handling is [UNVERIFIED]. | https://octopus.com/docs/argo-cd/steps/update-application-image-tags |
| F7 | "liveness /alive, readiness /_healthcheck" (sre-security §8) | `/_healthcheck` aggregates `LlmGateway`, `DataAccess` and `NeedsReboot` (`UIServiceRegistry.cs`). Anonymous `GET /_demo/setneedsreboot/true` is mapped in every environment (`ServerApplication.cs`). One request, or an Azure OpenAI outage, would remove every pod from service. | repo files cited |

### 2.2 Own round-1 claims retracted

- **Optional Codefresh GitOps Runtime and `codefresh-report-image`.** Retracted. Octopus states "The GitOps Cloud product is no longer available" (https://octopus.com/codefresh); promotions are disabled; the runtime's Argo CD trails upstream (pragmatist R3). Octopus Live Object Status and read-only Argo CD cover visibility.
- **[UNVERIFIED] wait for sync and health.** Resolved: step verification "Argo CD Application is healthy" (2026.1) and "Pull request merged" (2026.2), plus "Wait for Argo CD Applications" (180 s default) (https://octopus.com/docs/argo-cd/steps).
- **Pushing `ChurchBulletin.UI` and `ChurchBulletin.Script` to Octopus.** Retracted. Only Database and AcceptanceTests packages; images come through the ACR feed.
- **`GIT_COMMIT: ${{CF_REVISION}}`.** Wrong once Octopus CaC lives in the env repo.
- **Branch CI pushing preview images.** Replaced by the label-triggered `workorders/preview` pipeline.

## 3. Contested decisions

| # | Final stance | Argument | Strongest counter rejected, and why |
|---|---|---|---|
| C1 | **Change to Kustomize base + overlays.** Octopus writes only `images[].newTag`. Add `rollout-transform.yaml` to the base when Rollouts land. | Kustomize edits are documented as `newTag` only. The Helm mode rewrites every referenced values file, chart default included. Plain-YAML mode skips CRDs, so Rollouts need Kustomize's transformer config (Argo Rollouts Kustomize docs). CI publishes images only; there is no chart to version. | Helm OCI chart (gitops): real templating value, but its three-source layout makes Octopus a writer of the PR-only config repo (F6). |
| C2 | **Keep: Octopus DbUp step before the tag commit** for tdd/uat/prod, on the in-cluster Kubernetes worker. Previews: sync-wave Job running `churchbulletin.db-migrator … rebuild`. | A failed migration never produces a Git commit. The worker is in-cluster, so network and identity match a hook. Logs sit beside the approval. | Sync hook at wave -1 (gitops): ordering with the rollout is real, but hooks re-run on every sync and self-heal and have no approval point. |
| C3 | **Change to rolling update first**, then blue-green with Job-provider smoke analysis in a later phase. No canary for UI.Server. | One replica, an in-memory SignalR hub (octopus-architect §7) and version-coupled Blazor WASM assets. Analysis needs a metrics path that does not exist yet. | SRE canary with SLO analysis: needs a traffic router, a metrics pipeline and multi-replica state. |
| C4 | **Keep ApplicationSet PR generator only, deferred and opt-in by label.** No Octopus ephemeral environments. SQL Server container per preview. | F2: no custom-field or deprovision step in Codefresh. Preview releases would put Octopus credentials into PR-triggered pipelines. The generator cleans up on close. | Octopus record and tests for previews: CI already runs Playwright, and the extra PR-close path adds a third system to every PR. |
| C5 | **One env repo, `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh` (directive), with path-level writers** (§6.1). Octopus writes only `gitops/workorders/envs/**`; Codefresh writes nothing there. Design content as public-safe; recommend private to the user. | The stored Octopus credential covers `https://github.com/clearmeasure-aisf-sample-apps/*`, so a second repo adds no credential separation. Enforce paths with CODEOWNERS, a ruleset and the boundary lint. Env-repo pipelines exclude `gitops/workorders/envs/**` from triggers to prevent deploy-commit CI loops. | Config and releases repos (gitops): sound only with a repository-scoped writer credential, which the stored PAT is not. Revisit if Octopus moves to a single-repo GitHub App. |
| C6 | **During the parallel run, GitHub Actions `build-result` stays the only required check** and `codefresh/ci` is informational. **Flip** after 50 consecutive code-changing commits with equal verdicts and equal TRX totals, and p95 duration at most 1.2× GitHub. Then require `codefresh/ci` plus a reduced `build-result` for the ARM and Windows LocalDB jobs, which stay in GitHub Actions (a separately approved `.github/**` change). | Evidence-gated, and decoupled from the Prod runtime cutover: CI parity and AKS readiness are unrelated risks. | Pragmatist's "required only at P4": ties CI to Prod cutover, leaving two CIs ambiguous for months. |
| C7 | **Keep: Codefresh mints `2.5.<first-parent height>`** in one `version.sh` usable locally. Octopus release number = image tag = package version. Tags are ACR-locked. | Deterministic per commit and rerun-safe with `IGNORE_EXISTING`. Minor 2.5 sorts above every legacy `2.4.<run>`, and legacy keeps 2.4 until retirement. Parts stay under 65534. | `3.0.<height>` (pragmatist): a major bump signals an app breaking change; minor 5 already separates the series. |
| C8 | **Cut tenants.** | Nothing in the brief requests cohorts. Tenants add licensed objects and a cohort ApplicationSet. No CI impact either way. | Teaching value (octopus-architect): valid once a cohort requirement is written. |
| C9 | **Cut or defer Platform Hub.** | Enterprise tier. The platform account is Space Manager only. Required manual intervention, a separation-of-duties check step and admission policy cover the intent. | SRE policy enforcement: keep the Rego in the design record for when licensed. |
| C10 | **Recommendations to the user:** Octopus runbooks are the sole consumer. The Codefresh `azure-runtime-provisioner` context stays attached to no pipeline. Octopus Azure OIDC replaces the secret at the end of phase 1, after which the secret is deleted from both stores. | Codefresh executes branch-controlled YAML, and Octopus already offers plan, approval and audit. OIDC needs one Entra app-owner action. | "Delete after bootstrap" (sre-security): the replacement identity needs an Owner-granted role first, so a dated OIDC milestone is realistic. |
| C11 | **Octopus "Acceptance tests" step, tdd only**, on the Kubernetes worker. Execution container: Codefresh-built `platform/ci-dotnet`. Package `ChurchBulletin.AcceptanceTests` at the release version; TRX as Octopus artifacts. Failure fails the deployment, lifecycle blocks UAT, and status `platform/tdd` is posted. | Same Playwright build as CI. Results live in the release record. | Argo PostSync Job (gitops): re-runs on self-heal; results never reach Octopus. Agent-created Job (SRE): a deployment target is unnecessary for tests. |
| C12 | **Prerequisites, owned by the app team as board work items:** (a) before TDD on AKS: probes on `/alive`, `/_demo/*` gated outside non-prod, a database-only `/ready`; (b) before the Worker ships: `RemotableBus__ApiUrl`, a health endpoint (generic host, none today), Dockerfile under `platform/containers/worker/`; (c) before Prod cutover: workload-identity SQL for the app, an Entra option for the migrator, NServiceBus installers moved into migrations. | Each item gates a phase, not the whole platform. | Doing all app changes up front: blocks CI parity work that needs none of them. |

## 4. Non-negotiables

1. **Codefresh never holds deploy, cluster-write, GitOps-write or subscription credentials.** PR pipelines run identity-free; release credentials exist only in the master-pinned pipeline.
2. **One version, minted once from the commit by Codefresh,** used verbatim as image tag, package version and Octopus release number; release creation is idempotent.
3. **No gate is lost.** Every `build-result` input keeps running in Codefresh or GitHub Actions, and the required check moves only on measured agreement.

## 5. Proposed compromise for the top three conflicts

- **C1 packaging.** Octopus writes Kustomize `newTag` only. The base may inflate the GitOps architect's Helm chart through Kustomize `helmCharts` with Argo CD `--enable-helm` [UNVERIFIED OCI-chart behaviour]. Templating is kept; Octopus never touches values files. Fallback: plain Kustomize.
- **C2 migrations.** Octopus schedules migrations for named environments, running the same `churchbulletin.db-migrator` image as its execution container that the preview sync-wave Job runs. One artifact, two schedulers, and failures stop before Git.
- **C10 credential.** Honour the user's two stores. Octopus is the only active consumer; the Codefresh copy stays dormant until the Octopus OIDC milestone, when both copies are recommended for deletion. Interim guardrails: at most 90-day expiry, sign-in alerts, Owner-placed `CanNotDelete` locks on prod and legacy.

## 6. Naming and interface contracts

### 6.1 Repositories and paths

```text
clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh   (env repo; paths from its root)
  design/                                     design record
  argocd/{nonprod,prod}/                      root Application, AppProjects, ApplicationSets
  gitops/workorders/base/                     Kustomize base (Deployments, Services, HTTPRoute)
  gitops/workorders/envs/{tdd,uat,prod}/      kustomization.yaml; newTag written ONLY by Octopus
  gitops/workorders/previews/                 overlay for ApplicationSet workorders-previews
  octopus/terraform/                          space objects
  octopus/projects/workorders/                CaC base path, project workorders
  octopus/projects/workorders-infrastructure/ CaC base path, runbooks only
  terraform/{foundation,environment}/         Owner-applied / Contributor-applied via Octopus
  terraform/codefresh/                        codefresh_project, codefresh_pipeline (no secret values)
  codefresh/env-checks.yml                    credential-free IaC checks on this repo

ClearMeasureLabs/bootcamp-palermo-workorders  (app repo)
  platform/codefresh/pipelines/{ci.yml,preview.yml,ci-image.yml}
  platform/codefresh/scripts/{version.sh,changed-paths.sh,worktrees.sh,buildinfo.sh,Publish-BuildSummary.ps1}
  platform/codefresh/images/ci-dotnet/Dockerfile   platform/codefresh/version.env
  platform/containers/{worker,db-migrator}/Dockerfile
```

### 6.2 Codefresh

| Object | Name | Contract |
|---|---|---|
| Project | `workorders` | |
| Pipelines | `workorders/ci`, `workorders/release`, `workorders/preview`, `workorders/ci-image`, `workorders/env-checks` | Statuses `codefresh/ci`, `codefresh/release`, `codefresh/preview`, `codefresh/env-checks` |
| Runtimes | `<cf-runtime-pr>` (no identity), `<cf-runtime-release>` | Split of `<codefresh-runner-runtime>` |
| Registry integrations | `<acr-integration-release>` (scope `churchbulletin.*`), `<acr-integration-preview>` (scope `previews/*`) | Repository-scoped ACR tokens |
| Git integrations | Default GitHub (app repo); `github-aisf-sample-apps` (env repo, `env-checks` only) | Read and trigger only |

### 6.3 Octopus and Argo CD

| Tool | Object | Name |
|---|---|---|
| Octopus | Projects | `workorders` (Git credential `GitHub clearmeasure-aisf-sample-apps`), `workorders-infrastructure` |
| Octopus | Environments | `tdd`, `uat`, `prod`; runbook-only `infra-nonprod`, `infra-prod` |
| Octopus | Lifecycles and channels | `workorders-default` (tdd auto, uat, prod) with channel `Default` (`^2\.5\.\d+$`); `workorders-hotfix` (uat, prod) with channel `Hotfix` (`-hotfix\.\d+$`) |
| Octopus | Service account | `svc-codefresh-release`; OIDC issuer `https://oidc.codefresh.io`, subject pinned to the release pipeline and `scm_ref:master` |
| Octopus | Feeds, worker pools | built-in; `acr`; pools `aks-nonprod`, `aks-prod` |
| Octopus | Steps and runbooks | `Migrate database (DbUp update)`, `Update Argo CD image tags`, `Acceptance tests`, `Post status platform/tdd`, `Prod go/no-go`; runbooks `env-create`, `env-destroy`, `run-acceptance-tests`, `db-backup`, `db-restore-pitr` |
| Octopus | Variable sets | `Azure Runtime Provisioning`, `GitHub AISF Sample Apps` (existing); `workorders-environment-values` |
| Argo CD | Instances and AppProjects | `argocd-nonprod`, `argocd-prod`; `platform-addons`, `workorders-nonprod`, `workorders-prod`, `workorders-previews` |
| Argo CD | Applications | `workorders-{tdd,uat,prod}` in namespace `workorders-<env>`, annotated `argo.octopus.com/project: workorders`, `argo.octopus.com/environment: <env>`; ApplicationSet `workorders-previews` creates `workorders-pr-<n>` |

### 6.4 Images and packages (`<acr-name>.azurecr.io`)

| Artifact | Name | Version | Producer |
|---|---|---|---|
| UI, Worker, migrator images | `churchbulletin.ui`, `churchbulletin.worker`, `churchbulletin.db-migrator` | `2.5.<h>`, locked | `workorders/release` |
| Preview image | `previews/churchbulletin.ui` | `pr-<number>-<40-char sha>` (template `pr-{{.number}}-{{.head_sha}}`) | `workorders/preview` |
| CI toolchain | `platform/ci-dotnet` | by digest | `workorders/ci-image` |
| Octopus packages | `ChurchBulletin.Database`, `ChurchBulletin.AcceptanceTests` | `2.5.<h>` | `workorders/release` |

### 6.5 Variables, secrets, handoff

- **Codefresh contexts:** `workorders-ci` (`CI_SQL_SA_PASSWORD`, `AI_OPENAI_APIKEY`, `AI_OPENAI_URL`, `AI_OPENAI_MODEL`); `workorders-release` (`OCTOPUS_URL`, `OCTOPUS_SPACE`, `OCTOPUS_PROJECT`, `OCTOPUS_SERVICE_ACCOUNT_ID`, `GITHUB_APP_ID`, `GITHUB_APP_PRIVATE_KEY`). Existing `azure-runtime-provisioner` and `github-aisf-sample-apps-token` are recommended to stay attached to no pipeline.
- **Exported variables:** `VERSION`, `BUILD_BUILDNUMBER`, `CODE_CHANGED`, `IAC_CHANGED`, `PUBLISH`.
- **App configuration keys in manifests:** `ConnectionStrings__SqlConnectionString` (must start `Server=`, per sre-security R6), `ApplicationInsights__ConnectionString`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `RemotableBus__ApiUrl`, `AI_OpenAI_ApiKey`, `AI_OpenAI_Url`, `AI_OpenAI_Model`, `ASPNETCORE_ENVIRONMENT`.
- **Handoff arguments.**
  - `octopusdeploy-push-package`: the two nupkgs, `OVERWRITE_MODE: ignore`.
  - `octopusdeploy-push-build-information`: `PACKAGE_IDS` = the three images plus two packages; commits `HEAD^1..HEAD` from the app repo; `BuildUrl` = `CF_BUILD_URL`; `OVERWRITE_MODE: overwrite`.
  - `octopusdeploy-create-release`: `PROJECT: workorders`, `CHANNEL: Default`, `RELEASE_NUMBER` = `PACKAGE_VERSION` = `VERSION`, `GIT_REF: refs/heads/<env-repo-default-branch>`, no app SHA, `IGNORE_EXISTING: true`.
