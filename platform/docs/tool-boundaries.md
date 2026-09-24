# Tool boundaries

Every tool in this platform can deploy something. Three deployers is the biggest source of confusion for the people who run it (R1-P §6 D1), so each tool gets one verb and the overlapping features of the others stay off (ADR-D2). `scripts/checks/tool-boundaries.sh` enforces the rules on every push; the rule IDs below refer to it.

## One verb per tool

| Tool | Verb | Decides | Never |
|---|---|---|---|
| **Codefresh** | builds | Whether a commit is releasable: the `build.ps1` gates, version `2.5.<first-parent height>`, signed and locked images, the two NuGet packages, build information, the Octopus release (over OIDC) | Deploys, approves, syncs Argo CD, touches an app cluster, commits to any repo |
| **Octopus Deploy** | releases, promotes, approves, migrates, runs runbooks | When a release may enter an environment (lifecycles, channels, freezes), who approved it, whether its migration ran, which tags each environment runs (the pin commit), whether it verified; day-2 and environment runbooks | Applies Kubernetes objects, runs Helm or kubectl against app namespaces, creates releases from feed triggers |
| **Argo CD** | reconciles | How fast `main` becomes cluster state, and that the cluster stays that way (prune, self-heal) | Chooses versions (no Image Updater), holds a calendar (no sync windows), rolls back |
| **GitHub Actions** | gates merges (parallel run) | `build-result`, the only required check in phases 1–5, including ARM and Windows jobs | Takes on new platform duties; its workflows stay unchanged until phase 5 |

Two rules follow:
- **Git is the gate between Octopus and Argo CD.** Octopus writes exactly two `newTag` values per environment; Argo CD applies whatever `main` holds. Neither calls the other to change state; the gateway only reports health.
- **One writer per fact.** The version comes from Codefresh; the release record from Octopus; the running tags from the pin file; everything else in the environment repo from reviewed pull requests.

## Where the lanes meet

| Handoff | From → to | Contract | Checked by |
|---|---|---|---|
| Release creation | Codefresh → Octopus | OIDC login as `svc-codefresh-release`; packages, build information, release `${VERSION}` on channel `Default`, `IGNORE_EXISTING: true` (§7.7) | `consistency.sh` C16 |
| Pin commit | Octopus → environment repo | Step `update-argo-cd-image-tags`: direct commit to `main`, `images[].newTag` only (§7.6) | `consistency.sh` C08, `tool-boundaries.sh --audit-bot-commits` |
| Reconciliation | Environment repo → Argo CD | Applications `workorders-{tdd,uat,prod}` with automated prune and self-heal (§7.3) | `consistency.sh` C03 |
| Health report | Argo CD → Octopus | Gateway, read-only account `octopus`; verification "Argo CD Application is healthy", 900 s | `consistency.sh` C02, C06 |

## Consoles by role

Cognitive load is counted in consoles and credentials. Each role gets the fewest that let it do its job.

| Role | Uses | Access | Never needs |
|---|---|---|---|
| Developer or student | IDE, GitHub, the Codefresh build log, Octopus (read) | Octopus team `Developers` (view only) | Argo CD, writes to the environment repo, the Azure portal, kubectl |
| Release manager | Octopus | Team `Release Managers`: deploys to `uat` and `prod`, creates `Hotfix` releases, overrides `prod-weekend-freeze` with a reason | Codefresh, Argo CD, kubectl |
| UAT or prod approver | Octopus manual interventions | Teams `UAT Approvers`, `Prod Approvers` | Everything else |
| On-call | Octopus first (what changed, who approved, runbooks, redeploy previous), Argo CD read-only second, Azure Monitor for logs and the SLO alert | Team `SRE On-call`; Argo CD roles `sre-oncall` (nonprod: get, logs) and `oncall` (prod: get, logs, Rollout abort) | Codefresh; kubectl writes |
| Platform engineer | All four tools, Azure, pull requests to the environment repo | Team `Platform Engineers` (Space Manager); `@<org>/platform-owners` | kubectl writes to app namespaces: changes go through Git |
| Security owner | Azure (PIM), Entra, Key Vault, policy pull requests | `@<org>/security-owners` | Deploy rights |

## Features that stay off, and why

| # | Feature | Tool | Why it stays off | Rule |
|---|---|---|---|---|
| 1 | `deploy`, `helm`, `launch-composition` steps | Codefresh | A second deployer beside Octopus and Argo CD; nobody could say which one put a version in an environment | TB01 |
| 2 | `approval` steps | Codefresh | Approvals live in Octopus so there is one audit trail (ADR-D13) | TB01 |
| 3 | `argocd`, `kubectl`, `helm install/upgrade`, `az aks get-credentials` in pipelines | Codefresh | CI holds no cluster credentials; branch YAML is written by anyone who can push a branch | TB02 |
| 4 | GitOps Runtime and Promotions | Codefresh | Promotions are disabled in runtimes after 0.24.0 (E34), the GitOps Cloud product is gone (E35), and a runtime is a second Argo CD console | TB03 |
| 5 | Octopus API keys | Codefresh | An expired or rotated key caused 41 consecutive red runs (incident EP18); OIDC has nothing to expire (ADR-D8) | TB14 |
| 6 | Commits and pushes | Codefresh | Only Octopus (pins) and people (pull requests) write to the environment repo | TB16 |
| 7 | Stored contexts `azure-runtime-provisioner`, `github-aisf-sample-apps-token` attached | Codefresh | Branch-controlled YAML could exfiltrate them (R5) | TB13b |
| 8 | Image Updater | Argo CD | A second tag writer would race Octopus and skip approvals | TB04 |
| 9 | Sync windows | Argo CD | Two freeze calendars disagree; the calendar is `prod-weekend-freeze` in Octopus (ADR-D5) | TB05 |
| 10 | Manual sync and UI rollback of named Applications | Argo CD | Auto-sync blocks rollback (E40) and self-heal would undo it; rollback is Octopus "redeploy previous release" | RBAC: on-call roles have `get` and logs only |
| 11 | Octopus annotations on anything but the three named Applications; tenant annotations | Argo CD | The gateway would map add-ons or previews into Octopus deployments; there are no tenants (ADR-C8) | TB09 |
| 12 | Kubernetes YAML, Helm, Kustomize or kubectl steps against app namespaces | Octopus | Argo CD's self-heal would revert them, and Git would stop being the truth | TB07 |
| 13 | Trigger sync in the Argo CD step | Octopus | Keeps the gateway account read-only (E7); verification already waits for the step's own commit | TB10 |
| 14 | Feed-based or built-in release triggers | Octopus | A second release creator; duplicate release numbers caused incidents EP20 and EP21 | TB11 |
| 15 | Kubernetes deployment targets on app clusters | Octopus | Workers run migrations and tests; deployment targets would invite direct deploys | TB12 |
| 16 | The stored provisioner in the deployment project or in `infra-prod` | Octopus | A subscription-wide Contributor bearer secret must not reach prod (ADR-C10) | TB13a, TB13c, C14 |
| 17 | `latest` tags anywhere in desired state or pipelines | All | Legacy pushes `latest` from every branch (F12); desired state must name exact versions | TB06, C08 |
| 18 | Role assignments, locks, policy assignments in `terraform/environment` | Terraform | Contributor cannot create them (E36); every grant lives in the Owner-applied foundation (ADR-D10) | TB08 |
| 19 | `mutateDigest` in image verification | Kyverno | Rewriting images makes live state differ from Git, and self-heal keeps reverting it (ADR-D11) | TB15 |
| 20 | Bot commits that change more than `newTag` | Octopus machine user | The machine user bypasses review, so its commits are audited on every push to `main` | AUDIT |

## What the checks cannot see

- **Console actions.** A person clicking Sync in Argo CD or editing a variable in the Octopus UI leaves no file behind. RBAC (read-only roles), Octopus config-as-code on protected `main`, Octopus Git drift detection and Argo CD self-heal cover these.
- **Values in the Octopus database.** Sensitive variables, channels, triggers and lifecycles are not in Git (E26); `octopus/terraform` manages the non-sensitive ones.
- **Runtime identity misuse.** Workload identity subjects are exact (§7.8), and Kyverno admits only images signed by `workorders/release`.

## Changing a lane

A lane change is a design change. It updates the ADR in `design/platform-design.md`, the names in `contracts/platform-contracts.yaml`, and the rule in `scripts/checks/tool-boundaries.sh` in one pull request, reviewed by platform owners (and security owners when a credential or policy moves).
