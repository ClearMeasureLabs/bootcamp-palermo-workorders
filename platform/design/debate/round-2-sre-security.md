# Round 2 — SRE + Security & Compliance: critique and rebuttal

Role: `sre-security`. Sources were checked on 2026-09-23. Anything about the stored credentials or the environment repository is a **recommendation to the user**, not a decision.

## 1. Concessions

- **Probes (GitOps architect §7.1, §8).** Liveness and readiness use `/alive`, never `/_healthcheck`. `/_healthcheck` makes a live Azure OpenAI call ("Reply with OK") whenever its cache has expired, and does not cache Unhealthy results (`CanConnectToLlmServerHealthCheck.cs`). A model outage would therefore empty every endpoint. A database-only `/ready` becomes an app follow-up.
- **Analysis source (GitOps architect §2, §8).** An in-cluster Prometheus fed by the OTel collector replaces the round-1 Job-to-Azure-Monitor-workspace probe: secretless, low-latency, no pod churn. The Azure Monitor workspace keeps the SLO record through workload-identity remote-write.
- **Strategy (GitOps architect §6; Octopus architect §7).** Blue-green beats canary for UI.Server. Canary would split Blazor WASM clients across versions, and `RealtimeNotificationHub` is a per-process `ConcurrentDictionary`.
- **Sequencing (pragmatist D14, §8).** Rolling update first, Rollouts later.
- **Migrations (Octopus architect D4; pragmatist D6; Codefresh engineer D8).** DbUp runs on the in-cluster Octopus **Kubernetes worker**, not in a Job the agent creates. Octopus then needs no cluster write path at all.
- **Pre-migration copy (Octopus architect §4 step 10).** An Azure SQL copy before each prod migration, auto-deleted after 14 days.
- **Separation of duties (Octopus architect D13).** Octopus Approvals, with "Block approvals by the deployment creator", replaces the round-1 script guard once it leaves Public Preview.
- **Runbook scoping (Octopus architect §3).** Runbook-only `infra-nonprod` and `infra-prod` environments confine the provisioner account.
- **CI boundary (Codefresh engineer D16, §7.2).**
  - The `azure-runtime-provisioner` context is attached to no pipeline.
  - Build-step provenance is self-attested, so admission requires the signature, and provenance stays in Audit mode.
  - Versions come from first-parent commit height.
- **Required check (pragmatist D12, §2).** GitHub Actions keeps `build-result`. Pragmatist's `tool-boundaries.sh` lint is adopted and extended.
- **Cluster tier (pragmatist §3).** Nonprod on AKS Standard is acceptable because AKS Automatic uses node auto-provisioning and cannot be stopped. The Automatic controls must then be enabled explicitly.

## 2. Fact-check

| Claim | Correction | Source |
|---|---|---|
| GitOps §6: "Kustomize needs a CRD transformer for Rollout images" | Kustomize's built-in image field specs (`spec/template/spec/containers[]/image`) carry no `kind` filter. The Rollouts transformer config has no `images` section, only name references, labels, vars and replicas. | https://github.com/kubernetes-sigs/kustomize/blob/master/api/internal/konfig/builtinpluginconsts/images.go · https://argo-rollouts.readthedocs.io/en/stable/features/kustomize/ |
| GitOps §6: Helm works because "Octopus supports Helm tag paths" | True, but "Image tags are replaced in every Helm values file referenced by the Application — both the chart's default `values.yaml` and any files listed". The step also "will fail to execute if no git credentials exist for repositories referenced", so Octopus needs a credential for the config repo too. | https://octopus.com/docs/argo-cd/steps/update-application-image-tags |
| Codefresh §1/D10: optional runtime in "existing Argo CD" mode, off the critical path | Bring-your-own Argo CD mode needs an "Argo CD Admin API token" that "must be a non-expiring API key", plus a Codefresh API key. That puts a permanent admin credential on the prod reconciler. | https://codefresh.io/docs/docs/installation/gitops/runtime-install-with-existing-argo-cd/ |
| Codefresh §7.2/§9.1: Insights lead time comes from build-information commits | Insights defines lead time as "the time between the creation date of the release immediately following the previously successful release and the completion date of the deployment." | https://octopus.com/docs/insights |
| Pragmatist §3: "Codefresh gets only AcrPush via OIDC" | For push triggers, Codefresh's `sub` embeds `scm_user_name`. Entra federated credentials need an exact subject match. Flexible credentials accept only GitHub, GitLab and Terraform Cloud, and not managed identities. | https://codefresh.io/docs/docs/integrations/oidc-pipelines/ · https://learn.microsoft.com/en-us/entra/workload-id/workload-identities-flexible-federated-identity-credentials |
| Octopus §7: "create-release documents `GIT_REF`, no commit SHA" | `GIT_COMMIT` is documented. No custom-fields argument exists, so `PullRequestNumber` via the step stays unsupported. | https://codefresh.io/steps/step/octopusdeploy-create-release |
| GitOps §9: impersonation "beta 3.5.0" | Introduced in v2.13.0, status Beta. In 3.5 it covers all API operations, not only sync. | https://argo-cd.readthedocs.io/en/stable/operator-manual/feature-maturity/ · https://argo-cd.readthedocs.io/en/stable/operator-manual/upgrading/3.4-3.5/ |
| Status notes: Argo CD in Octopus (every paper flags it) and Octopus D13 "Octopus Approvals at GA" | 2026.2 still "evolves the Argo CD Preview"; general availability in 2026.4 is unverified. Octopus Approvals is Public Preview, so both need a fallback. | https://octopus.com/downloads/whatsnew/2026.2 · https://octopus.com/docs/approvals/octopus-approvals |
| Octopus §8: gateway Argo CD user "as documented" | Accurate, but the documented policy grants `applications, sync, *` and `logs, get, */*`, which reaches the add-ons. Scope it to `workorders-*/*` and drop `sync` unless Trigger sync is on. | https://octopus.com/docs/argo-cd/instances/argo-user |

Verified as stated:
- Selective sync skips hooks (Octopus architect). https://argo-cd.readthedocs.io/en/stable/user-guide/selective_sync/
- Ephemeral environments cannot use lifecycles, freezes, Insights or tenants, and have no native Argo CD support. https://octopus.com/docs/projects/ephemeral-environments
- Clusters that use node auto-provisioning cannot be stopped, and wildcard admission webhooks can reject a stop. https://learn.microsoft.com/en-us/azure/aks/start-stop-cluster
- Promotions are disabled after runtime 0.24.0. https://codefresh.io/docs/docs/promotions/promotions-overview/
- Octopus Terraform provider v1.20.0 was released on 2026-09-21. https://registry.terraform.io/providers/OctopusDeploy/octopusdeploy/latest

**Own claims retracted:**
- Round 1 §8: "readiness `/_healthcheck`" is withdrawn.
- Round 1 §6/§9: "the Codefresh GitOps runtime installs its own Argo CD" was incomplete; the bring-your-own mode exists, with the admin-token cost above.
- Round 1 §9: "ACR feed via OIDC `[UNVERIFIED]`" is now verified (Octopus 2025.2; needs an Entra app registration plus a federated credential): https://octopus.com/blog/oidc-external-feeds

## 3. Contested decisions

| # | Final stance | Argument | Strongest counter rejected |
|---|---|---|---|
| C1 | **Keep Kustomize** base + overlays. Octopus edits only `images[].newTag`; Rollout images resolve through built-in field specs. Release tags are ACR-locked; Kyverno uses `mutateDigest`. | One writable field per environment and no chart pipeline. Neither option pins digests, so admission verification is mandatory either way. | Helm OCI (GitOps). Rejected: the step rewrites every referenced values file, and multi-source forces Octopus credentials onto the config repo. |
| C2 | **Change:** Octopus DbUp step on the Kubernetes worker before the tag commit; prod DB copy first; an Argo CD PreSync **read-only schema-version check**. Previews: sync-hook `rebuild` into in-namespace SQL. | Approval context, PITR marker and one migration authority. The check fails closed if a tag lands without migrations. | Migrations as sync hooks (GitOps). Rejected: hooks rerun on every sync and are skipped on selective sync. |
| C3 | **Change:** phase 1 RollingUpdate (`maxUnavailable: 0`) plus a blocking Octopus verification; phase 2 blue-green with pre- and post-promotion analysis, after a realtime backplane and at least two replicas. | Least mixed-version exposure; instant abort. | Canary with analysis (round-1 SRE). Rejected: splits WASM clients and the in-memory hub; needs a traffic router. |
| C4 | **Keep, deferred:** PR generator only; maintainer label; same-repo branches; `workorders-previews` project (namespaced kinds); no Azure identity; private ingress (the Testing environment exposes `/_test/*`); quota; at most 3; 72h TTL. | Native cleanup and minimal credentials. | Adding Octopus ephemeral environments (Octopus). Rejected: no lifecycles, freezes, Insights or Argo CD support; a second preview model. |
| C5 | **Change:** one environment repo, `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh` (user directive); private recommended. Write matrix in §6, enforced by a push ruleset with path restrictions (private/internal repos, Team plan or above). | Path-scoped writes inside one repo. | Two repos now (GitOps). Kept as the fallback: if the repo is public, or push rulesets are unavailable, recommend a private `basic-environment-octopus-codefresh-releases` repo for pins. |
| C6 | **Change:** GitHub Actions `build-result` stays the required merge check during and after the parallel run. Codefresh `workorders/ci` is informational; `workorders/release` is the sole build of record and signer. ARM and Windows stay on GitHub Actions. | Fork commits never fire Codefresh `push.heads` triggers. A required `codefresh/ci` would block fork PRs or force untrusted code onto the in-network runner. | Flip to `codefresh/ci` after 50 agreeing builds (Codefresh). Rejected for fork safety. |
| C7 | **Keep:** Codefresh is the sole version authority: `2.5.<first-parent height>` = image tag = package = release; legacy stays `2.4.<run>`; numbers are never reused; `IGNORE_EXISTING` on reruns. | Deterministic and rerun-safe; ends the EP20/EP21 collision class. | `3.0.<height>` (pragmatist). Equally safe; `2.x` keeps continuity for `rc-` consumers. The chief architect decides. |
| C8 | **Cut** tenants. | Licensing and RBAC surface (students holding runbook rights) with no isolation gain. | Tenant per cohort (Octopus). Deferred to an ApplicationSet-generated cohort namespace if needed. |
| C9 | **Cut** Platform Hub from phase 1. Its intent is enforced by CaC PR review, required steps, Approvals and Kyverno; adopt it if an Enterprise license and system-team rights exist. | Enterprise-only; a Space Manager cannot author it. | Rego from day one (Octopus). Deferred, not rejected. |
| C10 | **Recommendation to the user:** use the provisioner only in the `platform-environments` runbooks, see list below. | A bearer secret with subscription-wide Contributor. | Held long-term by Octopus as the single holder (Octopus D16). Rejected as the end state. |
| C11 | **Keep:** Octopus runs Playwright on the Kubernetes worker after the "Argo CD application is healthy" check. Failure fails the deployment, and the lifecycle blocks UAT. TRX files become artifacts. The test identity reaches only the TDD database and secrets. `Degraded` fails in TDD. | One record; private TDD reachable. | GitHub Actions post-deploy tests (legacy). Rejected: split record; no network path. |
| C12 | **Prerequisites below.** | TDD on AKS starts on platform-side mitigations. | "All app changes first." Rejected: it blocks without a security gain. |

**C10 detail (recommendations to the user):**
- Use the provisioner only in the `platform-environments` runbooks.
- Restrict the account to `infra-*`.
- Include `Azure Runtime Provisioning` nowhere else.
- Attach the Codefresh `azure-runtime-provisioner` context to no pipeline, then delete it once the runbooks work.
- Guardrails: plan review plus approval, Owner locks, deny policies, sign-in alerts, expiry of 90 days or less.
- Replace it with an OIDC account before any prod environment is created.

**C12 prerequisites and owners:**
- Platform (no app change): `/alive` probes; ingress deny for `/_demo/*`, `/_healthcheck/detailed` and `/mcp` in UAT/Prod; schema-scoped NServiceBus grants; connection strings that start with `Server=`. The app's SQL workload identity needs no code change (SqlClient ≥ 6.1.1).
- App team, as work items needing approval:
  - `/ready` endpoint.
  - Worker health endpoint and Dockerfile (app repo `platform/docker/`).
  - Entra auth in the DbUp migrator (before prod cutover).
  - Realtime backplane (before phase-2 blue-green).
  - NServiceBus installers moved into the migration step (before prod).
  - Environment-gate `/_demo/*`.

## 4. Non-negotiables

1. **One path into prod.**
   - Octopus alone authorizes a prod change, with separation of duties.
   - Argo CD alone applies it.
   - No CI or human holds standing prod write.
   - Kyverno Enforce admits only release-pipeline-signed images before prod cutover.
2. **The subscription-wide provisioning credential never reaches CI, branch-controlled YAML or deployment projects.** The design attaches it nowhere else. Retiring it is a recommendation.
3. **Verification fails closed.**
   - No probe hits `/_healthcheck`.
   - No gate uses `continue-on-error`.
   - UAT/Prod post-deploy verification blocks.
   - `/_demo/*` is unreachable outside TDD and previews.

## 5. Proposed compromise for the top three conflicts

- **C2 (migrations).** Octopus keeps the write: DbUp on the worker, before the tag commit. GitOps gets in-cluster ordering: a PreSync Job with a read-only identity asserts that the DbUp journal holds the release's expected last script and fails the sync otherwise. Previews keep the sync-hook `rebuild`.
- **C1 (packaging).** Kustomize now. If a shared chart becomes necessary for sample-app templates, CI renders it into overlays (hydration), so Octopus still writes one `newTag` field and Argo CD never needs multi-source.
- **C6 (CI).** Both run through the parallel run. `build-result` stays required, keeping fork PRs on GitHub-hosted runners. Codefresh `release` alone publishes signed artifacts, and Kyverno trusts only its identity. Trimming duplicated GitHub gates later is an approved `.github/**` change.

## 6. Naming and interface contracts

**Environment repo** (`clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh`; paths from its root):

```text
design/                                      design record
argocd/clusters/{nonprod,prod}/root.yaml     app-of-apps (Application platform-root)
argocd/projects/                             platform-addons, workorders-nonprod, workorders-previews, workorders-prod
argocd/applicationsets/workorders-previews.yaml
argocd/addons/{kyverno,external-secrets,argo-rollouts,otel-collector,prometheus,octopus-argocd-gateway}/
apps/workorders/base/                        ui-server, worker, Services, HTTPRoute, ServiceAccounts, ExternalSecrets, ConfigMaps
environments/{tdd,uat,prod}/kustomization.yaml   Octopus writes images[].newTag only
environments/previews/                       preview overlay
policies/kyverno/   policies/octopus/        admission policies; Rego (used only if licensed)
observability/{rules,dashboards}/
octopus/projects/workorders/                 CaC base path (fallback .octopus/workorders/)
octopus/projects/platform-environments/      CaC runbooks
octopus/terraform/                           space resources (provider 1.20.x)
terraform/foundation/                        privileged: RGs, UAMIs, FICs, role assignments, locks, state
terraform/runtime-env/                       Contributor: AKS, SQL, Key Vault, App Insights (no role assignments)
runbooks/                                    human procedures
```

App repo: `platform/codefresh/pipelines/ci.yml`, `platform/codefresh/terraform/`, `platform/docker/{worker,db-migrator}.Dockerfile`.

**Write matrix:**

| Identity | May write |
|---|---|
| Octopus Git credential `GitHub clearmeasure-aisf-sample-apps` | `environments/{tdd,uat,prod}/kustomization.yaml`; `octopus/projects/**` through PR branches |
| Humans | Everything, through PR + CODEOWNERS |
| Argo CD | Nothing (a separate read-only credential; the stored PAT has write) |
| Codefresh | Nothing (the `github-aisf-sample-apps` integration is used read-only; the `github-aisf-sample-apps-token` context is attached to no pipeline) |

Recommendations to the user: narrow the stored PAT to selected repositories, give it 90-day expiry, and use a dedicated machine user.

**If the repo is public, the guardrails require:**
- Treat every identifier in it as public; secrets are referenced only through ESO, and Gitleaks plus push protection are on.
- Put the Octopus-written pins in a private releases repo, because push rulesets need private/internal repos.
- Keep private endpoints and Entra-only authentication as the real barrier.

**Octopus** (`<octopus-space>`):

| Object | Names |
|---|---|
| Projects | `workorders`; `platform-environments` (runbooks only) |
| Environments | `tdd`, `uat`, `prod`; runbook-only `infra-nonprod`, `infra-prod` |
| Lifecycles | `workorders-standard` (tdd auto → uat → prod), `workorders-hotfix` (uat → prod) |
| Channels | `Default`, `Hotfix` |
| Accounts | `Azure Runtime Provisioner` (existing; `infra-*` only); `azure-oidc-deploy-nonprod`, `azure-oidc-deploy-prod`, `azure-oidc-runbooks-prod`; target `azure-oidc-env-lifecycle` |
| Variable sets | `Azure Runtime Provisioning` (existing; `platform-environments` only); `GitHub AISF Sample Apps` (existing; unused by deployments); `WorkOrders Environment` |
| Worker pools | `aks-nonprod-workers`, `aks-prod-workers` |
| Service account | `svc-codefresh-release`, OIDC identity `codefresh-release-master` |
| Teams | `Platform Engineers`, `UAT Deployers`, `Prod Deployers`, `Prod Approvers`, `SRE On-call`, `Break Glass` |
| Steps (`workorders`) | `migrate-database`, `update-argo-image-tags` (healthy verification), `smoke-test`, `acceptance-tests` (tdd), `approve-prod`, `release-annotation` |
| Runbooks | `provision-runtime-env`, `destroy-runtime-env` (`platform-environments`); `db-copy-pre-release`, `db-restore-pitr`, `rotate-sql-migrator-secret` (`workorders`) |
| Variables | `Azure.KeyVault`, `Azure.SqlServerFqdn`, `Azure.SqlDatabase`, `App.BaseUrl`, `WorkerPool`, `Smoke.FailOnDegraded` |

**Argo CD:** Applications `workorders-{tdd,uat,prod}`, annotated `argo.octopus.com/project: workorders` and `argo.octopus.com/environment: <env>`. ApplicationSet `workorders-previews` generates `workorders-pr-<n>` in namespace `workorders-pr-<n>`. Named namespaces are `workorders-{tdd,uat,prod}`.

**Artifacts:**
- Images: `<acr-name>.azurecr.io/workorders/{ui-server,worker,db-migrator}:<version>` and `workorders/previews/ui-server:sha-<full-sha>`.
- Packages: `ChurchBulletin.Database`, `ChurchBulletin.AcceptanceTests`.
- Codefresh pipelines: `workorders/ci` (status `codefresh/ci`) and `workorders/release` (status `codefresh/release`).
- Codefresh contexts: `workorders-ci`, `workorders-release`.
- Codefresh runtimes: `<codefresh-runner-runtime>-ci` (push only to `workorders/previews/*`) and `<codefresh-runner-runtime>-release`.

**Identities and secrets:**
- UAMIs: `id-workorders-<env>-{ui-server,worker,migrator,eso,schemacheck}`, `id-platform-<cluster>-{kubelet,controlplane,kyverno}`, `id-cf-release-push`.
- ServiceAccounts: `workorders-{ui-server,worker,eso,schemacheck,acceptance}`.
- Key Vault `kv-workorders-<env>` holds `workorders-ai-openai-apikey`, `workorders-api-validation-key`, and `workorders-sql-migrator-password` (interim).
- ConfigMap keys: `ConnectionStrings__SqlConnectionString` (passwordless), `ApplicationInsights__ConnectionString`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `RemotableBus__ApiUrl`.
