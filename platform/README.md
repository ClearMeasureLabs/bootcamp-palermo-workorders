# Work Orders platform environment

Environment repo for the Work Orders app (`ClearMeasureLabs/bootcamp-palermo-workorders`): Argo CD, the GitOps overlays, Octopus config-as-code, Terraform, admission policies, the environment checks and the design record. Home: `clearmeasure-aisf-sample-apps/basic-environment-octopus-codefresh`.

> **Staging mirror.** Until the environment repo is attached (R1), this tree is staged at `platform/` in the app checkout as an exact mirror of the environment-repo root. Paths in every document are environment-repo paths (`gitops/…` is `platform/gitops/…` while staged); `app:` marks app-repo paths. After R17, copy `platform/` to the environment-repo root and delete it from the app repo in one commit per repo (ADR-D18).

Status: design and implementation sketch (phase P0). Nothing is provisioned. Every environment-specific value is a placeholder from design §7.1.

## One verb per tool

| Tool | Verb | Owns | Stays off |
|---|---|---|---|
| Codefresh | builds | Build of record on `master`, version `2.5.<first-parent height>`, signed images in ACR, NuGet packages and the release in Octopus (over OIDC); `platform-env/env-checks` for this repo | `deploy`, `approval`, `helm`, `launch-composition` steps; GitOps Runtime; Promotions |
| Octopus Deploy | releases, promotes, approves, migrates, runs runbooks | Lifecycles, channels, freezes, manual interventions, DbUp on in-cluster workers, the pin commit, verification, day-2 and environment runbooks | Kubernetes YAML and Helm steps against app namespaces |
| Argo CD | reconciles | Sync, prune and self-heal of `gitops/workorders/envs/<env>` into `workorders-<env>`; add-ons | Image Updater; sync windows |
| GitHub Actions | gates merges (parallel run) | `build-result`, the only required check in phases 1–5; ARM and Windows jobs | Unchanged: nothing new is added |

`scripts/checks/tool-boundaries.sh` enforces the lanes. Details, consoles by role and the reasons: [docs/tool-boundaries.md](docs/tool-boundaries.md).

## Commit to production in one paragraph

A master merge makes Codefresh mint `2.5.<first-parent height>`, run the `build.ps1` gates, push signed `workorders/ui-server`, `workorders/worker` and `workorders/db-migrator` images and the `ChurchBulletin.Database` and `ChurchBulletin.AcceptanceTests` packages, then create the Octopus release. Octopus deploys `tdd` automatically and `uat` and `prod` on approval. For each environment it reads secrets from that environment's Key Vault, runs DbUp on the environment's Kubernetes worker, commits two `newTag` values to `gitops/workorders/envs/<env>/kustomization.yaml`, waits until Argo CD reports Synced and Healthy at that commit, then checks `/_version` and `/_healthcheck`. In `tdd` only it runs the Playwright suite. The legacy GitHub Actions → Octopus → Container Apps path runs untouched until cutover.

## Layout and writers

Writers: **H** people through a reviewed pull request to `main`; **O-pin** Octopus, direct commit to `main`, `images[].newTag` only; **O-branch** Octopus UI edits of config-as-code on non-`main` branches, merged by H. Readers: Argo CD, Codefresh `env-checks`, Octopus, the environment Terraform.

```text
basic-environment-octopus-codefresh/          (staged at platform/ in the app checkout)
├── README.md  CODEOWNERS  .gitleaks.toml  .yamllint.yaml       H
├── contracts/platform-contracts.yaml         H     machine-readable design §7; read by the checks
├── design/platform-design.md, debate/        H     adjudicated design and debate record
├── docs/                                     H
│   ├── bootstrap.md  tool-boundaries.md  cutover-and-decommission.md  consistency-notes.md
│   ├── walkthroughs/01..05-*.md                    teaching labs 18–22
│   └── runbooks/*.md                               break-glass, rollback, PITR, rotation, SLO burn
├── scripts/checks/{tool-boundaries,consistency,validate-all}.sh     H   run by env-checks
├── codefresh/{pipelines/env-checks.yml, specs/platform-env-checks.yml}   H
├── .octopus/workorders/**                    H, O-branch   deployment process, variables, runbooks
├── .octopus/workorders-infrastructure/**     H, O-branch   env-plan, env-apply, env-destroy, …
├── octopus/terraform/                        H     Octopus objects outside config-as-code
├── argocd/                                   H     bootstrap values, cluster roots, projects, add-ons, apps
├── gitops/workorders/
│   ├── base/                                 H     shared manifests; no images: block
│   ├── components/                           H     roll-through changes (ADR-D6); bluegreen (phase 6)
│   ├── previews/                             H     phase 6
│   └── envs/{tdd,uat,prod}/
│       ├── kustomization.yaml                O-pin newTag only; H for anything else (break-glass)
│       └── config/                           H     per-environment configuration
├── policies/{kyverno,octopus}/               H     admission policies; inactive Platform Hub policy
└── terraform/
    ├── foundation/                           H     applied by a human Owner: every role assignment
    └── environment/                          H     applied only by Octopus runbooks
```

No other identity writes to this repo. Argo CD and Codefresh never write; Codefresh posts commit statuses only.

## Phase status

| Phase | Scope | Status (2026-09-24) | Exit criteria (summary) |
|---|---|---|---|
| P0 Design | Design, sketch, integration review | In progress | Every §11 file exists; validations pass or are recorded; Gitleaks clean; user accepts or amends §10 |
| P1 Foundation and CI parallel run | Foundation, Octopus Terraform, Codefresh objects; `workorders/ci` shadow; `workorders/release` creates releases | Not started | 10 agreeing master commits; each release created once; ≤ 1.2× GitHub duration; signed, locked images; no Octopus API key |
| P2 TDD on AKS | `env-apply` nonprod; Argo CD, gateway; TDD auto-deploy; WI-08 | Not started | ≥ 20 consecutive TDD releases, ≥ 90 % green; drills pass; 14 days of Kyverno audit; OIDC replaces the provisioner secret |
| P3 UAT and Worker | UAT with sign-off; Worker in tdd and uat; SLO alerts | Not started | Two approved UAT cycles; blocking UAT smoke; Worker 14 days clean |
| P4 Prod cutover | Prod environment over OIDC; WI-01/02/03/05; single migration owner; DNS | Not started | Rehearsal passed; PITR drill within RTO; 14 days of prod SLO; legacy rollback still possible |
| P5 Decommission | Legacy workflows, project, Container Apps and secrets retired | Not started | No legacy consumer; secrets deleted; docs updated |
| P6 Optional | Previews, blue-green, PreSync guard, Platform Hub, Octopus Approvals | Not planned | A measured need per item |

Checklists, evidence and rollback per phase: [docs/cutover-and-decommission.md](docs/cutover-and-decommission.md).

## Start here

| Need | Read |
|---|---|
| Why the platform looks like this | [design/platform-design.md](design/platform-design.md) (§2 decisions, §7 contracts, §9 phases, §10 recommendations) |
| Stand the platform up, in order, with an owner per step | [docs/bootstrap.md](docs/bootstrap.md) |
| Which tool does what, and which console each role uses | [docs/tool-boundaries.md](docs/tool-boundaries.md) |
| Move environments across, and retire the legacy path | [docs/cutover-and-decommission.md](docs/cutover-and-decommission.md) |
| Operate: break-glass, rollback, restore, rotation, SLO burn | [docs/runbooks/](docs/runbooks/) |
| Learn the platform (labs 18–22, each with an offline variant) | [01 Follow a commit](docs/walkthroughs/01-follow-a-commit.md) · [02 Schema and configuration change](docs/walkthroughs/02-schema-change.md) · [03 Promotion and hotfix](docs/walkthroughs/03-promotion-and-hotfix.md) · [04 Drift and rollback](docs/walkthroughs/04-drift-and-rollback.md) · [05 Environment lifecycle](docs/walkthroughs/05-environment-lifecycle.md) |
| Cross-package findings from the checks | [docs/consistency-notes.md](docs/consistency-notes.md) |

## Checks

`codefresh/pipelines/env-checks.yml` runs these on every push and posts `codefresh/env-checks`, which the `main` ruleset requires. Run them locally from the repo root:

```bash
scripts/checks/validate-all.sh all            # every check; missing tools are skipped with a warning
scripts/checks/validate-all.sh consistency    # names and shapes against contracts/platform-contracts.yaml
scripts/checks/tool-boundaries.sh             # one verb per tool
CI=true scripts/checks/validate-all.sh yaml   # CI mode: a missing tool fails
```

Tools: bash 4 or later (Alpine images and the macOS system bash lack it), git, yamllint, kustomize, kubeconform, terraform, gitleaks, python3 with PyYAML, and a Mermaid parser for `mermaid`. In Codefresh, `RENDER_DIR` on the shared volume carries rendered overlays from the `kustomize` step to the `kubeconform` step when they run in different images. Change a name in `contracts/platform-contracts.yaml` only in the same pull request that changes design §7.
