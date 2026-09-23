# Round 2 — Pragmatist: critique, concessions, contracts

| Field | Value |
|---|---|
| Role | pragmatist (DevEx and bootcamp teaching lead) |
| Round | 2 — cross-critique, 2026-09-23 |
| Binding inputs | User directives: provisioning credential stored in Octopus **and** Codefresh; sample-apps PAT stored in both; platform environment repo `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh` |
| Abbreviations | OA = octopus-architect, GA = gitops-architect, CE = codefresh-engineer, SRE = sre-security; §n = their round-1 section |

## 1. Concessions

1. **Previews run UI-only on SQLite + LearningTransport** (GA §3.3, §8). `DataContext` and `ShouldUseLearningTransport` both switch on `Data Source=`, and the `Testing` startup filter calls `EnsureCreated`: one pod, no SQL container, no migrations. Round 1's 2 GB SQL container is withdrawn.
2. **`/_healthcheck` must not be a probe** (GA §7.1, SRE intro). Confirmed in code (section 3, C12).
3. **Blue-green, not canary, is the first progressive strategy** (GA §6): no traffic router, no mixed Blazor WASM asset versions, instant abort.
4. **Version `2.5.<first-parent commit height>`** (CE §6 D6; OA §4 step 6). A minor bump is honest SemVer (README pattern 28); round 1's `3.0` implied a breaking change.
5. **Codefresh runs every Linux gate from phase 1 in shadow**, and its check becomes the required one after 50 agreeing builds; ARM and Windows stay on GitHub Actions (CE §1, §6 D12, D14).
6. **The `azure-runtime-provisioner` context is attached to no pipeline**; Codefresh runs only credential-free IaC checks (CE §6 D15, D16).
7. **A separate `workorders-infrastructure` project with runbook-only `infra-*` environments** keeps the provisioning account out of the deployment project (OA §3).
8. **Identity layers with an admin-held access layer, and `id-env-lifecycle` scoped to environment resource groups** rather than the whole subscription (SRE §3). Bootstrap-then-delete is the recommended end state for the client secret (SRE §6).
9. **Acceptance tests use the Codefresh-built CI image pinned to Playwright 1.54** on the in-cluster worker (CE §7.2), plus an on-demand `run-acceptance-tests` runbook (OA §8).
10. **SQL connection strings must start with `Server=`** (SRE R6), and a separation-of-duties guard (approver ≠ deployment creator) costs one script step (SRE §8).
11. **Pipelines `workorders/ci` and `workorders/release`, managed with the `codefresh-io/codefresh` Terraform provider** (CE §6 D4, §8.2).

## 2. Fact-check

| Claim (paper) | Corrected fact | Source |
|---|---|---|
| "Create-release documents `GIT_REF`, no commit SHA" (OA §7) | `GIT_COMMIT` and `IGNORE_EXISTING` are documented arguments; `OCTOPUS_ACCESS_TOKEN` is accepted | https://codefresh.io/steps/step/octopusdeploy-create-release |
| PR release carries custom field `PullRequestNumber` from Codefresh (OA §4 step 2) | The step lists no custom-field argument; that needs the Octopus CLI or API [UNVERIFIED flag] | same |
| "Octopus Approvals at GA" (OA D13) | Octopus Approvals is in Public Preview; plan on manual interventions | https://octopus.com/docs/approvals/octopus-approvals |
| Octopus writes only `release.yaml`, so it can "change which version runs, never how it runs" (GA §6) | For Helm sources, tags are replaced in "every Helm values file referenced by the Application — both the chart's default `values.yaml` and any files listed in `valueFiles`"; multi-source and OCI behaviour is undocumented | https://octopus.com/docs/argo-cd/steps/update-application-image-tags |
| Previews "need Helm anyway" (GA §7.2) | Argo CD Applications accept `spec.source.kustomize.images` overrides | https://argo-cd.readthedocs.io/en/stable/user-guide/kustomize/ |
| A failed Sync hook and an Octopus step fail the same way (GA §7.2) | Hooks are not run during selective sync, so a selective sync can roll pods without migrating | https://argo-cd.readthedocs.io/en/stable/user-guide/selective_sync/ |
| Preview tag `pr-{{.number}}-{{.head_short_sha}}` with Codefresh `sha8` (GA §8) | `head_short_sha` is 8 characters; `head_short_sha_7` also exists and matches Codefresh `CF_SHORT_REVISION` (7) | https://argo-cd.readthedocs.io/en/stable/operator-manual/applicationset/Generators-Pull-Request/ |
| Readiness `/_healthcheck` (SRE §8) | Unsafe: it calls the LLM, the database, and a `NeedsReboot` flag any anonymous `GET /_demo/setneedsreboot/{bool}` flips | `src/UI/Server/ServerApplication.cs`, `UIServiceRegistry.cs` |
| Codefresh → ACR via runner workload identity (SRE §6) | Inside docker-in-docker, still [UNVERIFIED]. ACR repository-scoped tokens work in every tier, with an optional expiry | https://learn.microsoft.com/en-us/azure/container-registry/container-registry-repository-scoped-permissions |
| AKS Automatic for the platform (SRE §3; GA §3.1) | Node auto-provisioning is preconfigured, so the cluster cannot be stopped; Deployment Safeguards use Azure Policy, a `Microsoft.Authorization` write Contributor lacks [UNVERIFIED under a Contributor caller] | https://learn.microsoft.com/en-us/azure/aks/intro-aks-automatic · https://learn.microsoft.com/en-us/azure/aks/start-stop-cluster |
| Agent script pods get cluster-wide admin by default (SRE §7) | Confirmed; `scriptPods.serviceAccount.targetNamespaces` restricts them. Same default for the Kubernetes worker is [UNVERIFIED], so restrict it too | https://octopus.com/docs/kubernetes/targets/kubernetes-agent/permissions |
| Confirmed, not errors | Provider `OctopusDeploy/octopusdeploy` 1.20.0 (2026-09-21) with `octopusdeploy_parent_environment`, channel `type = "EphemeralEnvironment"`, `execution_subject_keys` (OA); Rollouts 1.10.0 (GA); Codefresh ARM "only available to Enterprise customers" (CE); keyless `cosign.sign`, no SBOM or provenance field in the build step (CE) | https://registry.terraform.io/providers/OctopusDeploy/octopusdeploy/latest/docs · https://github.com/argoproj/argo-rollouts/releases · https://codefresh.io/docs/docs/installation/runner/arm-support/ · https://codefresh.io/docs/docs/pipelines/steps/build/ |

**Own round-1 claims retracted or corrected:**
- Readiness probe `/_healthcheck` (round-1 §8): withdrawn; `/alive` until a database-only `/ready` exists.
- "Codefresh gets only AcrPush via OIDC" (round-1 §3): withdrawn. The token's `sub` embeds `scm_user_name` (https://codefresh.io/docs/docs/integrations/oidc-pipelines/), and flexible federated credentials accept only GitHub, GitLab and Terraform Cloud tokens (https://learn.microsoft.com/en-us/entra/workload-id/workload-identities-flexible-federated-identity-credentials).
- "Only the 8-character `head_short_sha`" (round-1 §7): incomplete; see the table.
- `3.0.<all-ancestor count>`; SQL-container previews; Octopus config-as-code (CaC) in the app repo; a possibly public GitOps repo: replaced in C4, C5 and C7.
- Round-1 Q3 is answered: `OCTOPUS_ACCESS_TOKEN`, `GIT_COMMIT` and `IGNORE_EXISTING` exist.

## 3. Contested decisions

| # | Final stance | Argument | Strongest counter rejected, and why |
|---|---|---|---|
| C1 Packaging | **Keep Kustomize base + overlays.** Octopus edits only `newTag` in `gitops/apps/workorders/envs/<env>/kustomization.yaml`. Rollouts, when adopted, need the Rollouts transformer (`configurations: [rollout-transform.yaml]`, or `openapi` with Kustomize ≥ 4.5.5). | Octopus's documented Kustomize behaviour is one field in one file per environment. The Helm path rewrites every referenced values file, including the chart default. Plain-YAML scanning skips CRDs, so Rollout images must go through Kustomize. Local render with `kubectl kustomize` teaches well. | GA: signed OCI chart plus values precedence. Rejected: three-source Applications rest on undocumented Octopus behaviour (GA's own [UNVERIFIED]), and the chart needs its own release process. |
| C2 Migrations | **Keep the Octopus DbUp step** (`ChurchBulletin.Database update`) on the in-cluster Kubernetes worker, after approval and before the tag commit, for `tdd`, `uat`, `prod`. **Previews: no migration** (SQLite `EnsureCreated`). | The migration log sits beside the approval; the DDL credential never enters the cluster; order is explicit. Hooks skip selective syncs. | GA: a Sync hook at wave −1 orders migration with the rollout. Rejected: the worker already has network adjacency, and the compromise (section 5) adds a read-only guard. |
| C3 Progressive delivery | **Rolling update + readiness + Octopus health gate through phase 3; blue-green `ui-server` in Prod at phase 5** (`autoPromotionEnabled: true`, Job-provider smoke pre-promotion). Worker stays a Deployment. | One replica and bootcamp traffic give canary statistics nothing to measure; a paused Rollout is never Healthy, which would stall Octopus. | SRE: canary with an SLO AnalysisTemplate. Rejected until replicas ≥ 3 and traffic justify it; SLO burn-rate alerts proceed independently. |
| C4 PR previews | **Deferred to phase 5.** Then ApplicationSet PR generator only: maintainer label `preview`, same-repo branches, SQLite UI-only, tag `pr-<n>-<head_short_sha_7>`. | Disposable environments need no release record. The PR generator deletes on close. Nothing touches Azure or the provisioning credential. | OA: PR Preview channel + Octopus ephemeral environment. Rejected: two controllers materialise one namespace; the custom-field hand-off is undocumented in the Codefresh step; gateway scoping of ephemeral children is [UNVERIFIED]. |
| C5 GitOps repo | **One environment repo, `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh`** (user directive). **Recommend private**: push rulesets that restrict file paths exist only for private and internal repos (https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets). Write paths in section 6. | One repo, one mental model: `gitops/` says what runs, `.octopus/` and `terraform/` say how it is delivered. This repo's public `platform/` seed remains the teaching copy. | GA: separate config and releases repos. Rejected: the directive names one repo, and the Helm rewrite behaviour voids the promised separation anyway. |
| C6 CI and merge check | **Codefresh `workorders/ci` shadows every Linux gate from phase 1. `build-result` stays the only required check until 50 consecutive agreeing builds and updated factory tooling. Then `codefresh/ci` becomes the only required check**, and its final `gate` step also requires the GitHub `platform-matrix` workflow (ARM SQLite and Windows LocalDB) to be green for the same SHA via the check-runs API. | Keeps `docs/ci-single-gate.md` semantics: exactly one required check at every phase, all gates preserved. | Round-1 pragmatist (GitHub Actions gates forever) left Codefresh a thin release step and duplicated Linux work. Two required checks were rejected as a regression in developer experience. |
| C7 Version authority | **Codefresh mints `2.5.<first-parent height>`** (`master` is at 720 first-parent commits, so versions start near 2.5.72x). Octopus release = image tag = package version, with `IGNORE_EXISTING=true`. Branch builds use `-ci.<sha7>` and are never released. Hotfix releases are `<image version>-hotfix.<n>` over an existing image. Legacy keeps `2.4.<run>` in its own space until retired. | Deterministic, rerun-safe, computable locally, SemVer-greater than every 2.4.x. | All-ancestor count (round 1): jumps by PR size. Octopus `NextPatch`: the version is needed at compile time (`/p:Version`). |
| C8 Tenants | **Cut for phases 0–4.** Phase-5 gate: if cohort environments are funded, one tenant per cohort (never per student), deployed by Octopus with `argo.octopus.com/tenant`; never an ApplicationSet that pins versions itself. | Tenants are a paid add-on on Professional and Enterprise (https://octopus.com/pricing); ephemeral environments do not support tenants. | OA: tenant per cohort from the start. Rejected until a cohort requirement exists; it adds tenant concepts to a learner's first contact. |
| C9 Platform Hub | **Cut.** The pricing page lists it as Enterprise-only; its permissions are system-team only, while the platform account is Space Manager; one project gives no reuse. The fallback is a manual intervention first in the UAT and Prod processes plus the SRE separation-of-duties guard. | Governance is enforced where it lives today, at no licence risk. | OA and SRE: Rego policy for Prod go/no-go. Valuable at many projects; redundant at one. |
| C10 Provisioning credential | **User decision respected; recommendations follow.** Only `workorders-infrastructure` runbooks include `Azure Runtime Provisioning`; the Codefresh context stays unattached. Phase 1: an Owner applies the foundation, including `id-env-lifecycle` (Contributor on environment resource groups, federated to the Octopus issuer). Phase-2 exit: runbooks use that OIDC account, and the user deletes the client secret and both stored copies. Until then: expiry ≤ 90 days, sign-in alerts, prod CanNotDelete locks, no prod destroy runbook. | One holder in use, a dated exit, guardrails the Contributor principal cannot remove. | Holding the secret indefinitely (OA D16): no deletion date. A Codefresh provisioning pipeline exposes a subscription credential to branch YAML. |
| C11 Acceptance tests | **Octopus TDD step** after "Argo CD Application is healthy" and the health gate. Runs `ChurchBulletin.AcceptanceTests` on the non-prod worker in the CI image, with `StartLocalServer=false`, the TDD URL and TDD connection string (tests query through `IBus`). TRX goes to artifacts. A failure fails the TDD deployment, and the lifecycle blocks UAT. | Evidence lives on the release; one gate owner. | GA: tests inside Rollouts analysis. Rejected: 10–20 minutes and database credentials inside a rollout; analysis stays a smoke check. |
| C12 App prerequisites | **Platform-side now:** probes `/alive` on 8080; HTTPRoute denies `/_demo/*` and `/_healthcheck/detailed` in UAT and Prod; Worker image `platform/containers/worker.Dockerfile` (base `dotnet/aspnet:10.0`, needs `RemotableBus__ApiUrl`). **App-side, owned by the app team via the board:** database-only `/ready` and gated `/_demo/*` before Prod cutover; a Worker health endpoint, and an `X-Api-Key` header in the Worker's `RemotableBus` before `ApiKeyAuthentication__Enabled=true`; Entra auth in the DbUp console and NServiceBus installers moved into migration before passwordless SQL. App SQL via `Authentication=Active Directory Workload Identity` in a `Server=` string is config only. | Nothing blocks TDD on AKS; two items block Prod; one blocks passwordless SQL. | SRE: passwordless SQL and gated `/_demo` before any AKS environment. Rejected for TDD, which holds no user data; enforced before Prod. |

## 4. Non-negotiables

1. **One writer per state.** Octopus alone writes image tags in `gitops/apps/workorders/envs/*/kustomization.yaml` and alone promotes and approves. No Codefresh deploy, approval or promotion steps; no Image Updater; no sync windows. The boundary lint and the push ruleset enforce it.
2. **The live path stays untouched until measured exit criteria pass.** No database is shared between legacy and new paths before the Prod cutover window, and exactly one migration owner exists at cutover.
3. **Exactly one required merge check at every phase, and an unchanged inner loop:** `PrivateBuild.ps1`, `AcceptanceTests.ps1`, Aspire AppHost, and pipelines that call `build.ps1`.

## 5. Proposed compromise for the top three conflicts

- **Packaging and repo shape (C1/C5; GA vs OA/CE/SRE/pragmatist).** Kustomize in the single environment repo, keeping GA's layering: `base/` (reviewed), `envs/<env>/config/` (reviewed patches), `envs/<env>/kustomization.yaml` (Octopus-only `newTag`). A push ruleset restricts the Octopus identity to that file pattern. The Rollouts transformer is vendored in `base/` the day Rollouts arrive. Previews use `kustomize.images`. GA keeps separation by path, reviewed configuration and one writer per file, without a chart pipeline or undocumented multi-source behaviour.
- **Migrations (C2; GA vs rest).** Octopus migrates, and Argo CD guards. An optional phase-3 PreSync Job, read-only, fails the sync when the DbUp journal lacks the latest script name baked into the image label. Manual, selective or out-of-band syncs then cannot roll pods ahead of the schema. The guard needs only the app's read identity.
- **Progressive delivery (C3; GA blue-green vs SRE canary vs defer).** Rolling update now; blue-green for Prod `ui-server` in phase 5, with a Job-provider smoke analysis and no Prometheus dependency. SRE's burn-rate alerts go live in phase 3 as the post-deploy signal, not an in-rollout gate. Canary is reconsidered at ≥ 3 replicas.

## 6. Naming and interface contracts

**Repositories and paths**

| Item | Contract |
|---|---|
| App repo `ClearMeasureLabs/bootcamp-palermo-workorders` | `platform/codefresh/pipelines/ci.yml`; `platform/codefresh/version.env` (`MAJOR=2`, `MINOR=5`); `platform/codefresh/terraform/`; `platform/containers/worker.Dockerfile`; `platform/checks/{tool-boundaries.sh,consistency.sh}`; `platform/docs/` (onboarding, walkthroughs, labs 18–22); `platform/environment-seed/` (public, placeholder copy of the environment repo) |
| Environment repo `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh` | `gitops/apps/workorders/{base,envs/{tdd,uat,prod},previews}/`; `argocd/{nonprod,prod}/` (pinned Argo CD values, AppProjects, root Application, ApplicationSets, gateway values); `.octopus/workorders/` and `.octopus/workorders-infrastructure/` (CaC); `terraform/{octopus,azure/foundation,azure/environment}/`; `docs/design/` (design record) |
| Octopus CaC location | Proposed for the environment repo: the stored Git credential is restricted to that org, and no path collides with the live `.octopus/`. If the chief architect rules the process "app-side", a ClearMeasureLabs credential is required |

**Write paths per identity**

| Identity | May write | Never |
|---|---|---|
| Octopus Git credential `GitHub clearmeasure-aisf-sample-apps` | `gitops/apps/workorders/envs/*/kustomization.yaml` | anything else. Recommendation to the user: narrow the credential's repository restriction from `/*` to this repo, and back it with a machine user or GitHub App outside the ruleset bypass list; a PAT acts as its owning user |
| Humans | everything, by PR with CODEOWNERS | direct pushes |
| Argo CD | nothing (read-only) | — |
| Codefresh | commit statuses only | GitOps paths. `github-aisf-sample-apps-token` stays unattached to workorders pipelines |

**Octopus** (space `<octopus-space>`, project group `Work Orders`)

| Object | Names |
|---|---|
| Projects | `workorders` (deployments), `workorders-infrastructure` (runbooks only) |
| Environments | `tdd`, `uat`, `prod`; runbook-only `infra-nonprod`, `infra-prod` |
| Lifecycles | `workorders-standard` (tdd automatic → uat → prod); `workorders-hotfix` (uat → prod); `infrastructure` |
| Channels | `Default` (created only by Codefresh); `Hotfix` (created by release managers) |
| Runbooks | `env-create`, `env-destroy` (infra-nonprod only), `db-backup`, `db-restore-pitr`, `rotate-sql-credential`, `run-acceptance-tests`, `restart-workloads` |
| Worker pools | `k8s-nonprod`, `k8s-prod` (Kubernetes worker, no Kubernetes API role) |
| Service account | `svc-codefresh-release` (OIDC, issuer `https://oidc.codefresh.io`; create release, push packages and build information) |
| Teams | `Platform Engineers`, `UAT Approvers`, `Prod Approvers`, `Developers` (read-only) |
| Accounts | `Azure Runtime Provisioner` (existing; infrastructure only), `azure-oidc-env-lifecycle`, `azure-oidc-deploy-nonprod`, `azure-oidc-deploy-prod` |
| Library variable sets | `Azure Runtime Provisioning`, `GitHub AISF Sample Apps` (existing); `Work Orders Environment` (new, non-sensitive per-environment values) |
| Variables | `App.BaseUrl`, `Health.FailOnDegraded` (`True` only in tdd, #9017), `Sql.Server`, `Sql.Database`, `WorkerPool`, `Argo.VerificationTimeoutSeconds` (900) |

**Argo CD, Kubernetes, Codefresh, artifacts**

| Object | Names |
|---|---|
| Instances | `argocd-nonprod`, `argocd-prod` (upstream 3.5.x, one gateway each) |
| AppProjects | `platform`, `workorders`, `workorders-previews` |
| Applications | `workorders-tdd`, `workorders-uat`, `workorders-prod`, annotated `argo.octopus.com/project: workorders` and `argo.octopus.com/environment: <env>` |
| ApplicationSet | `workorders-previews` → `workorders-pr-<n>` |
| Namespaces | `workorders-{tdd,uat,prod}`, `workorders-pr-<n>`, `argocd`, `octopus-argocd-gateway`, `octopus-k8s-worker`, `codefresh-runner` |
| Deployments | `ui-server`, `worker` (Aspire AppHost names); Service `ui-server:8080` |
| Codefresh | project `workorders`; pipelines `workorders/ci` (status `codefresh/ci`), `workorders/release` (status `codefresh/release`); contexts `workorders-ci`, `workorders-release`; existing `azure-runtime-provisioner` and `github-aisf-sample-apps-token` attached to none of them |
| Images | `<acr-name>.azurecr.io/workorders/ui-server:<version>`, `.../workorders/worker:<version>`, tags locked; previews `.../workorders/pr-ui-server:pr-<n>-<sha7>`. Octopus package ID = repository name |
| NuGet packages | `ChurchBulletin.Database`, `ChurchBulletin.AcceptanceTests` (consumed); `ChurchBulletin.UI`, `ChurchBulletin.Script` (pushed, unused) |
| Versions | `2.5.<first-parent height>`; hotfix `-hotfix.<n>`; branch `-ci.<sha7>` |
| Status contexts | `codefresh/ci`, `codefresh/release`, `platform/tdd` (Octopus; requires a ClearMeasureLabs credential with commit-status write — recommendation); legacy `build-result`, `Deploy to TDD` |
| Manifest keys | `ConnectionStrings__SqlConnectionString` (must start with `Server=`), `ApplicationInsights__ConnectionString`, `RemotableBus__ApiUrl` (`http://ui-server.workorders-<env>.svc:8080/api/blazor-wasm-single-api`), `ASPNETCORE_ENVIRONMENT`, `AI_OpenAI_*` (Key Vault) |
| Probes | liveness, readiness and startup `/alive`, until `/ready` exists |
