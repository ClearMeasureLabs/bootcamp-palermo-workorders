# Codefresh pipelines for Work Orders

Codefresh **builds**: it runs the Linux gates, mints the version, builds, signs and attests the images, pushes the Octopus packages and creates the Octopus release. It never deploys, approves, waits for a deployment or touches a cluster. Octopus releases and promotes. Argo CD reconciles. The source of these rules is the platform design, `platform/design/platform-design.md`: §7.7 (contracts), ADR-C6, C7, D8, D11 and D17.

During phases 1–5, GitHub Actions `build-result` stays the only required check. `.github/workflows/build.yml` does not change.

## Layout

| Path | Purpose |
|---|---|
| `pipelines/ci.yml` | `workorders/ci`: the gates for branch pushes |
| `pipelines/release.yml` | `workorders/release`: the same gates, then packages, images, supply chain and the Octopus handoff |
| `pipelines/preview.yml` | `workorders/preview` (phase 6): preview images for labelled pull requests |
| `pipelines/ci-image.yml` | `workorders/ci-image`: the toolchain image `platform/ci-dotnet` |
| `specs/*.yml` | Pipeline specs: triggers, runtime, contexts, YAML location, concurrency |
| `scripts/version.sh` | `MAJOR.MINOR.<first-parent height>` on master, `…-ci.<sha7>` elsewhere |
| `scripts/changed-paths.sh` | Changed paths for docs-only detection (master: `HEAD^1..HEAD`; branches: merge base with `origin/master`) |
| `scripts/gate.sh` | `build-result` semantics over the step results |
| `scripts/buildinfo.sh` | Octopus build information and the release notes file |
| `scripts/stage-built.sh` | Lean Docker contexts for the three images |
| `scripts/supply-chain.sh` | SBOM and provenance attestations, ACR tag lock |
| `images/ci-dotnet/Dockerfile` | SDK 10, pwsh, Playwright 1.54 Chromium, go-sqlcmd, az CLI, Gitleaks, Syft, cosign, crane |
| `version.env` | `MAJOR=2`, `MINOR=5` |
| `../containers/worker/Dockerfile`, `../containers/db-migrator/Dockerfile` | Worker and DbUp migrator images |

The environment repo pipeline `platform-env/env-checks` lives in that repo under `codefresh/` (staged at `platform/codefresh/` until the repo is attached).

## Pipelines

| Pipeline | Trigger | YAML revision | Runtime | Contexts | Registry integration | Status |
|---|---|---|---|---|---|---|
| `workorders/ci` | `branch-push`: `push.heads`, branch regex `/^(?!master$).+/` | Triggering revision | `<cf-runtime-ci>` | `workorders-ci` | none (pulls with `acr-platform-ci`) | `codefresh/ci` (informational) |
| `workorders/release` | `master-push`: `push.heads`, `/^master$/` | `master` (pinned) | `<cf-runtime-release>` | `workorders-ci`, `workorders-release` | `acr-workorders-release` | `codefresh/release` |
| `workorders/preview` (phase 6) | `pullrequest.opened`, `.synchronize`, `.labeled` [VERIFY]; forks off | Triggering revision | `<cf-runtime-ci>` | none | `acr-workorders-preview` | `codefresh/preview` |
| `workorders/ci-image` | Cron `0 6 * * 1`; master push touching `.codefresh/images/**` | `master` (pinned) | `<cf-runtime-release>` | none | `acr-platform-ci` | — |
| `platform-env/env-checks` | `push.heads`, every branch of the environment repo | Triggering revision | `<cf-runtime-ci>` | none | none | `codefresh/env-checks` (required on `main`) |

Concurrency and termination:
- `workorders/ci` and `workorders/preview`: a new build cancels older builds of the same branch (`terminationPolicy: branch/onCreate`).
- `workorders/release` and `workorders/ci-image`: `concurrency: 1`. Builds queue and are never cancelled.
- `platform-env/env-checks`: no termination policy, so every push to `main` finishes its bot-path audit.

Contexts (values never in Git):
- `workorders-ci` (secret): `CI_SQL_SA_PASSWORD` (a throwaway password for the service container), `AI_OPENAI_APIKEY`, `AI_OPENAI_URL`, `AI_OPENAI_MODEL` (a CI-only, low-budget key).
- `workorders-release` (config): `OCTOPUS_URL`, `OCTOPUS_SPACE`, `OCTOPUS_PROJECT=workorders`, `OCTOPUS_SERVICE_ACCOUNT_ID`, `ACR_REGISTRY=<acr-name>.azurecr.io`.
- The stored contexts `azure-runtime-provisioner` and `github-aisf-sample-apps-token` are attached to no pipeline.

Registry integrations hold repository-scoped ACR tokens: `acr-workorders-release` (`cf-workorders-release`, `workorders/*`), `acr-workorders-preview` (`cf-workorders-preview`, `workorders-previews/*`) and `acr-platform-ci` (`cf-platform-ci`, `platform/*`). All three share the `<acr-name>.azurecr.io` domain, so freestyle steps that pull `platform/ci-dotnet` set `registry_context: acr-platform-ci`.

Git integrations: `<cf-git-integration-app>` (the existing default, ClearMeasureLabs) for the app repo; the stored `github-aisf-sample-apps` for the environment repo. Both are used only to read and to trigger.

Runtimes (ADR-D17, recommended to the user): `<cf-runtime-ci>` runs branch-controlled YAML and has no cloud identity; `<cf-runtime-release>` runs on a separate, tainted node pool. A branch build can therefore never poison the layer cache of a release build.

## Registering the specs

Prerequisites (bootstrap, `platform/docs/bootstrap.md`): both runtimes, the three registry integrations, both contexts and the projects `workorders` and `platform-env` exist.

```sh
# App repo root
codefresh create pipeline -f .codefresh/specs/workorders-ci.yml
codefresh create pipeline -f .codefresh/specs/workorders-release.yml
codefresh create pipeline -f .codefresh/specs/workorders-ci-image.yml
codefresh create pipeline -f .codefresh/specs/workorders-preview.yml     # phase 6 only

# Environment repo root
codefresh create pipeline -f codefresh/specs/platform-env-checks.yml

# Later changes to a spec
codefresh replace pipeline -f .codefresh/specs/workorders-release.yml
```

Before registering, replace the placeholders in the specs and pipelines: `<cf-runtime-ci>`, `<cf-runtime-release>`, `<cf-git-integration-app>`, `<acr-name>`, `<ci-image-version>`, every `<…-digest>`, `<…-version>` and `<…-sha256>`, and `<platform-bots-author-regex>`.

Order:
1. Register `workorders/ci-image` and run it once. After `smoke` passes, put the new tag into `StepImage.CiDotnet` (Octopus, environment repo) and into the `platform/ci-dotnet` references in `ci.yml`, `release.yml` and `preview.yml` by pull request.
2. Register `workorders/ci` and `workorders/release`. After the first release build, copy the exact OIDC `sub` into the Octopus identity `codefresh-release-master` of `svc-codefresh-release`, wildcarding only the user segment [VERIFY].
3. Register `platform-env/env-checks`. After its first report, require `codefresh/env-checks` on `main`.

The spec field names follow the CLI spec format (<https://codefresh-io.github.io/cli/pipelines/spec/>) and the JSON tags in `codefresh-io/terraform-provider-codefresh` (`codefresh/cfclient/pipeline.go`). The termination-policy entry `{type: branch, event: onCreate}` is the provider's mapping of `on_create_branch`.

## Step flow

`workorders/ci` and the first half of `workorders/release`:

```text
main_clone (full depth)
  └─ prepare: VERSION, BUILD_BUILDNUMBER, CODE_CHANGED, IS_RELEASE; one git worktree per gate
       ├─ build_sql      main clone      Build + mssql service, then CRAP
       ├─ build_sqlite   wt/sqlite       Build -UseSqlite
       ├─ code_analysis  wt/analysis     restore, format style, format analyzers, build -warnaserror
       ├─ qodana         wt/qodana       jetbrains/qodana-cdnet:2026.2, baseline, threshold 0
       ├─ security_scan  wt/security     Gitleaks, NuGet vulnerable and deprecated (advisory)
       └─ acceptance     wt/acceptance   Invoke-AcceptanceTests + mssql service
            └─ gate: gate.sh (finished on all six)
```

Every gate is skipped when `CODE_CHANGED=false`; `gate` then passes, as `build-result` does. Only `release.yml` continues:

```text
gate ─ package (master, CODE_CHANGED=true) ─ stage_images ─┬─ ui_image ───────┐
                                                            ├─ worker_image ───┼─ supply_chain ─ octopus_token ─ octopus_login
                                                            └─ migrator_image ─┘       ─ octopus_packages ─ octopus_build_info ─ octopus_release
```

Shared volume: the NuGet cache lives at `${CF_VOLUME_PATH}/.nuget/packages`. Each step links `/tmp/nuget-packages` to it, because `build.ps1` pins `NUGET_PACKAGES=/tmp/nuget-packages` outside GitHub Actions (F11). Reports go to `${CF_VOLUME_PATH}/reports/<step>/`, which `prepare` clears at the start of each build.

Handoff (contract §7.7, in order): `obtain-oidc-id-token:1.2.3` (`AUDIENCE` = the service account ID), `octopusdeploy-login:1.0.0`, `octopusdeploy-push-package:1.0.1` (`ChurchBulletin.Database`, `ChurchBulletin.AcceptanceTests`; `OVERWRITE_MODE: ignore`), `octopusdeploy-push-build-information:1.0.1` (five package IDs, commits `HEAD^1..HEAD`, `OVERWRITE_MODE: overwrite`), `octopusdeploy-create-release:1.0.1` (`PROJECT: workorders`, `CHANNEL: Default`, release number and package version `VERSION`, `GIT_REF: refs/heads/main`, no `GIT_COMMIT`, `IGNORE_EXISTING: true`).

Re-runs mint the same `VERSION`. Octopus accepts that, but the locked tags reject a second image push. After a failure past `supply_chain`, restart the build from the failed step [VERIFY] instead of re-running it.

## Parity map: GitHub Actions to Codefresh

| `build.yml` job | Codefresh step | Difference |
|---|---|---|
| `changes` | `prepare` | Same classifier (`detect-code-changes.sh --from-list -`), same fail-open rule. Master diffs `HEAD^1..HEAD` instead of `event.before..HEAD`. |
| Set Version (`2.4.<run>`) | `prepare` (`version.sh`) | `2.5.<first-parent height>`; `-ci.<sha7>` off master (ADR-C7) |
| `build-linux` (CI build, CRAP) | `build_sql` | A step-scoped SQL Server service container with `SQL_EXTERNAL=true` instead of Docker on the runner |
| `build-sqlite` | `build_sqlite` | — |
| `integration-build-arm` | — | Stays on GitHub Actions (ARM builds are Enterprise-only, E22) |
| `code-analysis` | `code_analysis` | Same four commands |
| `qodana` | `qodana` | Docker image pinned to the baseline release instead of a floating tag; no SARIF upload to code scanning |
| `build-windows` | — | Stays on GitHub Actions (Windows builds are incubating, E23) |
| `security-scan` (disabled) | `security_scan` | Runs, but advisory; the Gitleaks binary replaces the action |
| `acceptance-tests` | `acceptance` | Chromium baked into the image; 30-minute timeout as before |
| `acceptance-tests-arm` | — | Stays on GitHub Actions |
| `build-result` | `gate` | `gate.sh`; `security_scan` advisory |
| `docker-build-image-for-churchbulletin-ui` | `stage_images`, `ui_image` (release only) | Same `built/` extraction (F6). Tags `<VERSION>` and `sha-<sha7>`, locked, signed; no branch images |
| — | `worker_image`, `migrator_image`, `supply_chain` | New |
| `publish-octopus` | `package`, `octopus_*` (release only) | OIDC instead of an API key; the new space; only the Database and AcceptanceTests packages |
| `publish-github-packages` | — | Legacy path only |
| Test Reporter and artifacts | `${CF_VOLUME_PATH}/reports/` | TRX files are kept on the volume; publishing them for the ADR-C6 comparison is not built yet |

`deploy.yml` has no Codefresh counterpart: Octopus and Argo CD replace it.

## Boundary rules

Enforced by review and by `scripts/checks/tool-boundaries.sh --app-repo <app checkout>` in the environment repo:
- No `deploy`, `approval`, `helm` or `launch-composition` steps. No `argocd`, `kubectl`, `helm install/upgrade` or `az aks` commands against any cluster.
- No Codefresh GitOps Runtime or Promotions objects.
- No `latest` tag anywhere. Release tags are `<VERSION>` and `sha-<sha7>`, locked in ACR.
- No Octopus API key in any pipeline: `OCTO_API_KEY` stays with the legacy path until decommission.
- Codefresh reads repositories and posts statuses. It never commits or pushes.
- Release credentials exist only in `workorders/release`, whose YAML is pinned to `master`. Branch-controlled YAML (`ci`, `preview`) gets no release context and runs on the runtime without cloud identity.
- `azure-runtime-provisioner` and `github-aisf-sample-apps-token` stay unattached.
- Octopus images and packages are pushed only by `workorders/release`; nothing else creates Octopus releases.

## Running the scripts locally

```sh
bash .codefresh/scripts/version.sh                     # needs a full clone; unshallows or fails
bash .codefresh/scripts/changed-paths.sh | bash .github/scripts/detect-code-changes.sh --from-list -
GATE_build_sql=success GATE_qodana=failure CODE_CHANGED=true \
  bash .codefresh/scripts/gate.sh --advisory security_scan build_sql qodana
bash .codefresh/scripts/buildinfo.sh --out /tmp/buildinfo.json --release-notes-out /tmp/notes.md
# After `. ./build.ps1; Build` and `Package-Everything` with the same BUILD_BUILDNUMBER:
bash .codefresh/scripts/stage-built.sh --version "$BUILD_BUILDNUMBER"
```

## Contract gaps and open verifications

For the chief architect:
1. **Release notes.** §7.7 asks for `RELEASE_NOTES` whose first line is `app-commit: ${{CF_REVISION}}`. `octopusdeploy-create-release` 1.0.1 renders the argument unescaped into a plain YAML scalar of its generated step, where `: ` does not parse. `release.yml` passes `RELEASE_NOTES_FILE` instead; `buildinfo.sh` writes the same first line. The Octopus side is unaffected, but `consistency.sh` check C16 expects `RELEASE_NOTES`.
2. **Registry credentials for `supply_chain`.** Syft, cosign, crane and the tag lock need the `cf-workorders-release` token as a Docker config. Freestyle steps cannot use a registry integration, and §7.7 defines no context key for the token. Until a source is decided (for example two keys in `workorders-release`, which makes it a secret context), `supply_chain` fails closed and the handoff does not run.
3. **`PLATFORM_BOT_AUTHORS`.** The bot-path audit fails on `main` when this is unset (`CI=true`). §7 names no value, so the env-checks spec carries `<platform-bots-author-regex>`.
4. **Marketplace step images.** The Octopus steps run `octopuslabs/octopus-cli` at a floating tag inside their step definitions; pinning `type: …:1.0.1` does not pin the CLI.
5. **Account-wide registry integrations.** Branch YAML could name `acr-workorders-release`. Mitigations: repository-scoped tokens, locked tags, Kyverno's signer identity (the release pipeline only) and ABAC on integrations [VERIFY]. `registry_context: acr-platform-ci` in branch builds also exposes a token that can push `platform/*`; a pull-only integration would need a new §7.7 name.
6. **UI base image.** The root `Dockerfile` uses `mcr.microsoft.com/dotnet/aspnet:10.0` without a digest; this package may not change it.

[VERIFY] before relying on them:
- `CF_OIDC_REQUEST_URL` and `CF_OIDC_REQUEST_TOKEN` inside freestyle steps (`supply-chain.sh` requests the `sigstore` audience itself).
- The `steps.<name>.result` values (`gate.sh` treats anything but `success` as failure).
- The event name `pullrequest.labeled` and the format of `CF_PULL_REQUEST_LABELS`.
- Two parallel steps each with an `mssql` service container (separate compositions).
- Tag lock through the token's data plane (`az acr repository update`, metadata write).
- Cron timezone (UTC assumed); the hook's access to the volume; restart from a failed step.
- Runtime size: one build runs five .NET builds, Qodana and two SQL Server containers in parallel on `<cf-runtime-ci>`.
