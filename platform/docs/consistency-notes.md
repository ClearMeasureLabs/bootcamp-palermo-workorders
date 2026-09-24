# Consistency notes

Cross-slice findings from running `scripts/checks/validate-all.sh all` (which runs `tool-boundaries.sh` and `consistency.sh`) over every staged package, plus a few manual probes of interfaces the scripts do not cover. Every finding names the package that owns the fix (design §11.6).

| Field | Value |
|---|---|
| Pass | **First pass, partial tree.** Two packages were still being written. A final cross-slice pass follows once every package is staged. |
| Run | 2026-09-24T00:52:42Z, local mode (`CI` unset: a missing tool would have been skipped; none was missing) |
| Command | `scripts/checks/validate-all.sh all` from the staged root `platform/`; `--app-repo` auto-detected (the app checkout) |
| Tools | yamllint 1.38.0; kustomize v5.8.1; kubeconform 0.8.0 (`-strict -ignore-missing-schemas`, built-in schemas plus the datreeio CRDs catalog); terraform 1.16.4 (`fmt -check` only: `TF_VALIDATE` off because providers were not downloaded); gitleaks 8.28.0; mermaid 11 parser (jsdom); python3 3.11 with PyYAML 6.0.1 |
| Overall | 7 of 8 sub-commands pass; `consistency` fails (70 pass, 10 fail, 20 warn, 1 skip). Seven of the ten failures are files not yet staged. |

## Package completeness at the run

| Package | Staged / expected | Status | Missing |
|---|---|---|---|
| codefresh-engineer | 11 / 21 | **Incomplete** | `app:.codefresh/README.md`; `app:.codefresh/pipelines/{release,preview,ci-image}.yml`; `app:.codefresh/specs/workorders-{ci,release,preview,ci-image}.yml`; `codefresh/pipelines/env-checks.yml`; `codefresh/specs/platform-env-checks.yml` |
| gitops-architect | 38 / 38 | Complete | — |
| octopus-architect | 31 / 31 | Complete | — |
| sre-security | 26 / 38 | **Incomplete** | `policies/**` (6 files); `docs/runbooks/*.md` (5 files); `.gitleaks.toml` |
| pragmatist | 16 / 16 | Complete | — (this file completes the package) |

No package root holds a file outside its §11 list.

## Results by sub-command

| Sub-command | Result | Detail |
|---|---|---|
| `yaml` | PASS | 40 files. 1 warning: `gitops/workorders/envs/prod/config/kustomization.yaml:22`, line of 202 characters (limit 200, warning level). |
| `kustomize` | PASS | `gitops/workorders/envs/{tdd,uat,prod}` and `gitops/workorders/previews` build. `policies/kyverno/overlays/*` not staged. |
| `kubeconform` | PASS | 95 resources in 23 files (4 rendered overlays, 19 raw `argocd/` manifests): 95 valid, 0 invalid, 0 skipped. Runs on placeholder stand-ins (see checker notes). |
| `terraform` | PASS | `fmt -check -recursive` on `terraform/` and `octopus/terraform/`. `validate` not run (provider downloads, `TF_VALIDATE=true`). |
| `mermaid` | PASS | 9 Markdown files with Mermaid blocks, including the design and debate record. |
| `boundaries` | PASS | 18 of 18 rules checked, 0 violations, including `app:.codefresh/` and `app:containers/`. Bot-path audit skipped: it runs on `main` only. |
| `consistency` | FAIL | Findings below. |
| `secrets` | PASS | gitleaks: no leaks, 1.13 MB scanned. Default rules, because `.gitleaks.toml` was not staged yet. |

## Findings

Status **open** means the owner has work to do; **pending** means the file was not staged at the run.

| # | Check | Finding | Owner | Action | Status |
|---|---|---|---|---|---|
| CN-01 | C16 | Specs `app:.codefresh/specs/workorders-{ci,release,preview,ci-image}.yml` missing, so contexts, runtimes, revision pin, concurrency and handoff arguments could not be checked | codefresh-engineer | Finish the package | Pending |
| CN-02 | C16 | `platform-env/env-checks` (spec and pipeline) not staged, so the `validate-all.sh` sub-commands, `CI=true` and `PLATFORM_BOT_AUTHORS` could not be checked | codefresh-engineer | Finish the package; see CN-12 | Pending |
| CN-03 | C18 | `policies/kyverno/base/verify-release-signatures.yaml` and both overlays missing. The Argo CD side already points at `policies/kyverno/overlays/{nonprod,prod}` (C18 pass) | sre-security | Finish the package | Pending |
| CN-04 | secrets | `.gitleaks.toml` missing; the scan used the default rules | sre-security | Finish the package | Pending |
| CN-05 | C22 | `env-plan`, `env-apply` and `env-destroy` pass the worker registration token as `TF_VAR_<worker-registration-token-variable>`. `terraform/environment/variables.tf` declares it as `octopus_worker_registration_token` (sensitive, ephemeral) | octopus-architect | Use `TF_VAR_octopus_worker_registration_token` in all three runbooks | Open |
| CN-06 | manual | `terraform/environment` declares `argocd_repo_read_credential` (ephemeral, default `null`) to seed `argocd-repo-creds` before ESO exists (§5.2: "passed once" at bootstrap). No runbook passes it, and §7.2 names no Octopus variable that could hold it. A first `env-apply` would bootstrap Argo CD without repo access | chief-architect, then octopus-architect and sre-security | Decide the holder and name (for example a sensitive, prompted variable of `env-apply`) and add it to §7.2 | Open: decision |
| CN-07 | manual | The runbooks read `Octopus.Action.Terraform.VarFiles = "#{Environment.Class}.tfvars"` from Git, but §6.1 stages only `terraform/environment/{nonprod,prod}.tfvars.example`. The layer has 24 required variables without defaults (identity IDs, subnet IDs, DNS zone IDs, vault and SQL names, chart versions) | chief-architect, then sre-security and octopus-architect | Decide whether non-secret `terraform/environment/{nonprod,prod}.tfvars` are committed (after R2 makes the repo private) or fed from `WorkOrders Infrastructure`. `docs/bootstrap.md` step 2 follows the runbooks and flags the decision | Open: decision |
| CN-08 | manual | `db-backup` and `db-restore-pitr` run in `uat` with `azure-oidc-deploy-uat`, but §5.2 grants SQL DB Contributor to the prod deploy identity only. sre-security made the grant configurable and reported it | chief-architect | Amend §5.2 (uat also needs SQL DB Contributor) or drop `uat` from both runbooks in §7.2 | Open: decision |
| CN-09 | C14 (warn) | `StepImage.CiDotnet` is also defined in `.octopus/workorders-infrastructure/variables.ocl` (for `configure-db-principals-<env>` and `rotate-sql-passwords`); §7.2 places it in `workorders` only | octopus-architect / chief-architect | Add it to §7.2 for both projects, or move it into a library variable set | Open |
| CN-10 | C21 (warn) | `<docker-hub-feed>`: the Terraform steps pull `octopusdeploy/worker-tools:<worker-tools-version>` from a feed that §7.2 does not define (only the built-in feed and `acr-workorders`) | chief-architect, then octopus-architect | Add a container feed to §7.2 and `octopus/terraform/feeds.tf`, or mirror worker-tools into ACR | Open: decision |
| CN-11 | C21 (warn) | Placeholders outside §7.1: `<argocd-nonprod-host>`, `<argocd-prod-host>`, `<argocd-{cluster}-host>` (gitops-architect and sre-security use the same names); `<entra-group-object-id-{developers,platform-engineers,sre-on-call}>` (gitops-architect); `<OCTOPUS_HOST>` beside §7.1's `<OCTOPUS_URL>` (gitops-architect); `<previews-hostname-suffix>` (gitops-architect, phase 6); `<provisioner-secret-expires-on>`, `<40-hex sha>`, `<client-id-of-id-*>` (octopus-architect); `<name>`, `<object_id>` in descriptions (sre-security) | chief-architect | Add the Argo CD hostnames, the Argo CD RBAC group object IDs and `<OCTOPUS_HOST>` to §7.1 (then to the contracts); the rest are descriptive and need no change. The Entra administrator also creates those RBAC groups: bootstrap step 2 lists only the SQL admin groups | Open |
| CN-12 | checker | The bot-path audit (§6.2) needs the machine user's commit identity. `validate-all.sh boundaries` on `main` with `CI=true` fails until `PLATFORM_BOT_AUTHORS` is set in `platform-env/env-checks`; the identity Octopus commits with is [VERIFY] | codefresh-engineer; user (R3 machine user) | Set `CI=true` and `PLATFORM_BOT_AUTHORS` in the env-checks spec or pipeline; use step images with bash 4 or later | Open |
| CN-13 | C11 (warn) | No ExternalSecret reads `argocd-sso-client-secret` | gitops-architect | None while SSO uses federation (Q15); add the ExternalSecret only if federation is unavailable | Accepted |
| CN-14 | C19 (warn) | `terraform/environment` never names `environment-{class}.tfstate`; the runbooks pass `-backend-config=key=#{Terraform.StateKey}` | — | None: consistent through the library variable set | Accepted |
| CN-15 | yaml (warn) | Line of 202 characters in `gitops/workorders/envs/prod/config/kustomization.yaml:22` | gitops-architect | Optional wrap | Accepted |
| CN-16 | CODEOWNERS | §6.1 marks `policies/` as security-owned; §6.2 requires platform owners. `CODEOWNERS` lists both teams, so either one approves | chief-architect | Decide which team alone approves `policies/**` | Open: decision |

## Cross-package interfaces (§11.6)

| Interface | Result |
|---|---|
| `terraform/environment/bootstrap.tf` (sre-security) reads `argocd/bootstrap/*` (gitops-architect) | Pass: every referenced file exists for both clusters (C19) |
| `argocd/clusters/*/addons/kyverno.yaml` (gitops-architect) points at `policies/kyverno/overlays/*` (sre-security) | Argo CD side passes (C18); the overlays are pending (CN-03) |
| `codefresh/pipelines/env-checks.yml` (codefresh-engineer) calls `scripts/checks/validate-all.sh` (pragmatist) | Pending (CN-02, CN-12) |
| Octopus runbooks (octopus-architect) run `terraform/environment` (sre-security) | Open: CN-05, CN-06, CN-07 |
| Pin files (gitops-architect) written by the Octopus step (octopus-architect) | Pass: exact §7.6 shape in all three environments (C08); step packages `workorders/ui-server`, `workorders/worker` on feed `acr-workorders` equal the Kustomize `images[].name` repositories (C07) |
| Workload identity: federated subjects (sre-security) against ServiceAccounts (gitops-architect) | Pass (manual): `ui-server`, `worker`, `workorders-eso`, `external-secrets/external-secrets` and `kyverno/kyverno-admission-controller` match on both sides |
| NetworkPolicy sources (gitops-architect) against worker namespaces (sre-security) | Pass (manual): rendered overlays allow `octopus-worker-{tdd,uat,prod}`; no `{env}` template survives rendering |

What passed in full: Octopus annotations only on the three named Applications, with slugs equal to Octopus environments (C02, TB09); Applications, AppProjects, namespaces and the root Application against §7.3–§7.4 (C03–C06); rendered probes on `/alive`, identities, ESO objects and redirects (C09); every connection string starting with `Server=` (C10); Key Vault names against every ExternalSecret (C11); runbook scopes, with `env-destroy` limited to `infra-nonprod` (C12); the twelve deployment steps in order with their scoping (C13); project variables (C14); all 43 §7.2 object names in `octopus/terraform`, with stored objects looked up and never created (C15); configuration overlays (C17); the previews ApplicationSet (C20); and all 18 tool-boundary rules.

## Checker notes (pragmatist)

- **Placeholder stand-ins for kubeconform.** §7.1 placeholders are not valid DNS names, so the first run failed on the HTTPRoute hostnames. `validate-all.sh kubeconform` now validates copies where each `<placeholder>` becomes `ph-<placeholder>`. The committed files are unchanged.
- **Readiness on `/ready`.** C09 accepts `/ready` for the readiness probe only, as §7.9 allows once WI-01 exists (walkthrough 02 teaches the switch).
- **Contract additions, taken from the design text:** `promptedVariables` (`RestorePointInTime`, from the §7.2 runbook table), `notationTokens` (`<env>`, `<rg>` and similar notation the design uses) and `placeholderPatterns` (`<*-chart-version>` from §7.1, and version, digest and checksum pins).
- **False positives removed during this pass:** a status name read as a package ID; non-secret names (`<argocd-sso-app>`, a policy assignment name) read as Key Vault secrets; the provisioner's display name inside a warning message read as an account reference; names built with HCL interpolation (`rg-workorders-${e}`).
- **Not checkable by file:** console actions, Octopus database values and runtime identity misuse ([tool-boundaries.md](tool-boundaries.md), "What the checks cannot see").

## Decisions needed from the chief architect

1. CN-06: holder and name of the bootstrap-only Argo CD repo credential for the first `env-apply`.
2. CN-07: commit non-secret `terraform/environment/{nonprod,prod}.tfvars`, or another delivery path for the environment layer's required inputs.
3. CN-08: SQL DB Contributor for `id-octopus-deploy-uat`, or no uat scope for `db-backup` and `db-restore-pitr`.
4. CN-10: the container feed for `octopusdeploy/worker-tools`.
5. CN-11: new §7.1 placeholders (Argo CD hostnames, Argo CD RBAC group object IDs, `<OCTOPUS_HOST>`) and who creates those Entra groups.
6. CN-09: `StepImage.CiDotnet` in both projects.
7. CN-16: code owners of `policies/**`.

## Reproduce

```bash
cd platform   # the environment-repo root after R17
PATH="<tools>/bin:$PATH" \
MERMAID_VALIDATOR=<path to a mermaid-parsing node script> \
scripts/checks/validate-all.sh all
```

The final cross-slice pass repeats this command once codefresh-engineer and sre-security are complete, and replaces the status column above.
