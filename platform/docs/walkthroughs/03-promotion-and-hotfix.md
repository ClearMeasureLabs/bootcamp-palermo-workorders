# Lab 20: Promotion, Approvals, Freezes and the Hotfix Channel

**Curriculum Section:** Section 06 (Operate/Execute - Release Management)
**Estimated Time:** 40 minutes
**Type:** Process
**Builds on:** Lab 07 (pull requests), Lab 18 (follow a commit)

---

## Objective

Explain how a release moves from `tdd` to `prod`: which transitions are automatic, which need a person, which person, when a deployment is blocked, and how an urgent fix reaches production without skipping the audit trail. Replace the legacy `force_skip_tdd` habit with the auditable `Hotfix` channel.

## Background

All promotion decisions live in Octopus (ADR-D13): one audit trail, one calendar. Codefresh never deploys or approves; Argo CD never chooses a version.

**Lifecycles and channels**

| Channel | Lifecycle | Phases | Who creates releases | Release number |
|---|---|---|---|---|
| `Default` | `workorders-standard` | `TDD` (automatic from phase 2) → `UAT` (manual) → `Prod` (manual) | Only `svc-codefresh-release` (Codefresh, over OIDC) | `2.5.<n>`, equal to the package versions |
| `Hotfix` | `workorders-hotfix` | `UAT` → `Prod` | Team `Release Managers` | `<package-version>-hotfix.<n>`, over packages that already exist |

Both channels accept only releases from Git reference `refs/heads/main` of the environment repo. `Default` also rejects package versions with a pre-release tag, so a branch build (`-ci.<sha7>`) can never be promoted.

**Human and policy gates**

| Step | Where | Who | What it proves |
|---|---|---|---|
| `hotfix-justification` | `Hotfix` channel, first step | `Release Managers` | Why the normal path is skipped |
| `prod-go-no-go` | `prod`, first step | `Prod Approvers` | Someone accountable accepted the release for prod |
| `sod-guard` | `prod`, before any change | Script | The go/no-go approver is not the person who started the deployment |
| `uat-signoff` | `uat`, after verification | `UAT Approvers` | UAT was tested; the lifecycle then allows `Prod` |
| `prod-weekend-freeze` | `prod`, Saturday–Sunday [VERIFY recurring support] | Override: `Release Managers`, with a reason | Weekday releases, matching `app:docs/release-cadence.md` |

`sod-guard` fails when `Octopus.Action[prod-go-no-go].Output.Manual.ResponsibleUser.Id` equals `Octopus.Deployment.CreatedBy.Id`. It replaces Octopus Approvals' "block approvals by the deployment creator" until that feature reaches GA (R20).

**Steps per environment** (project `workorders`, twelve steps, each scoped):

| Environment | Default channel | Hotfix channel adds |
|---|---|---|
| `tdd` | `read-deployment-secrets` → `migrate-database` → `update-argo-cd-image-tags` → `verify-version` → `smoke-test` → `acceptance-tests` → `report-commit-status` | (not in the lifecycle) |
| `uat` | `read-deployment-secrets` → `migrate-database` → `update-argo-cd-image-tags` → `verify-version` → `smoke-test` → `uat-signoff` | `hotfix-justification` first |
| `prod` | `prod-go-no-go` → `sod-guard` → `read-deployment-secrets` → `db-copy-pre-release` → `migrate-database` → `update-argo-cd-image-tags` → `verify-version` → `smoke-test` | `hotfix-justification` first |

## Steps (online)

Students observe with the `Developers` role; a release manager drives any action.

### Step 1: Read the lifecycle

Project `workorders` → Lifecycles. Find the three phases of `workorders-standard` and the two of `workorders-hotfix`. Note which phase deploys automatically.

### Step 2: Follow a release from TDD to UAT

Open a release that reached `uat`. In its UAT deployment, find `uat-signoff`: who signed off, when, and with which note. Confirm that no prod deployment of that release started before the sign-off.

### Step 3: Read a prod deployment

Open a prod deployment. Find the `prod-go-no-go` responsible user, the deployment creator and the `sod-guard` output. Open the `db-copy-pre-release` log and note the copy's name (`<database>-pre-<release with dots replaced>`) and its `expires-on` tag (14 days).

### Step 4: Read the freeze

Deployment freezes → `prod-weekend-freeze`. Note its schedule, its scope (`prod`, project `workorders`) and the audit entries of any override.

### Step 5: Walk a hotfix (release manager demonstrates)

1. The fix merges to `master` through the normal pull request; Codefresh runs every gate and creates release `2.5.<n>` on `Default`, which deploys to `tdd` as usual.
2. The release manager creates a `Hotfix` release `2.5.<n>-hotfix.1` that selects packages `2.5.<n>`.
3. The hotfix deploys to `uat`: `hotfix-justification` asks why; then the UAT steps and `uat-signoff`.
4. The hotfix deploys to `prod`, overriding the weekend freeze with a reason if needed.
5. The pin commit writes the **package** version `2.5.<n>`, not the release number; `verify-version` checks that `/_version` starts with the package version.

## Offline variant: predict every handoff

Use only the staged files: `octopus/terraform/{lifecycles,channels,freezes,teams}.tf`, `.octopus/workorders/deployment_process.ocl`, `.octopus/workorders/variables.ocl`, `contracts/platform-contracts.yaml`. Predict the outcome of each scenario, then check.

| # | Scenario | Prediction: what happens, which steps run, who acts | Check in |
|---|---|---|---|
| S1 | Tuesday 10:00, Codefresh creates release `2.5.740`. | | `lifecycles.tf` (`tdd_auto_deploy`) |
| S2 | Wednesday, a release manager deploys `2.5.740` to `uat`. | | `deployment_process.ocl` scoping |
| S3 | Friday 16:00, release manager A starts the `prod` deployment; prod approver B approves go/no-go. | | Steps 2–9 |
| S4 | Same as S3, but A is also in `Prod Approvers` and approves the go/no-go. | | `sod-guard` |
| S5 | Saturday 09:00, a regular `prod` deployment of `2.5.740`. | | `freezes.tf` |
| S6 | Sunday, a production bug. The fix merges; Codefresh creates `2.5.741`. How does it reach `prod`, with which release number, and what does the pin file say afterwards? | | `channels.tf`; §7.6 |
| S7 | A developer tries to create a release on `Default` by hand. | | `channels.tf`; team `CI Release Publishers` |
| S8 | A `Hotfix` release built from package `2.5.735` while `prod` already ran the contract migration from `2.5.738`. | | Lab 19 Part A |
| S9 | Which Octopus object replaces `force_skip_tdd` from `app:.github/workflows/deploy.yml`? | | ADR-D13 |

<details>
<summary>Answer key</summary>

- **S1.** From phase 2 (`tdd_auto_deploy = true`), the release deploys to `tdd` automatically: secrets, migration, pin commit, Argo CD healthy verification, version, smoke, acceptance tests, status. `uat` and `prod` wait for a person.
- **S2.** `read-deployment-secrets` → `migrate-database` → `update-argo-cd-image-tags` → `verify-version` → `smoke-test` → `uat-signoff` (team `UAT Approvers`). Acceptance tests never run in `uat` (they wipe their database, ADR-C11). The release cannot enter `prod` until the UAT phase succeeds, which includes the sign-off.
- **S3.** `prod-go-no-go` (B approves) → `sod-guard` passes (B is not A) → secrets → pre-release database copy → migration → pin commit to `envs/prod` → healthy verification → version → smoke.
- **S4.** `sod-guard` fails because the approver created the deployment. It runs before any secret read, copy, migration or commit, so nothing has changed; another prod approver must approve a new deployment.
- **S5.** `prod-weekend-freeze` blocks it. A release manager may override with a recorded reason; routine releases wait until Monday.
- **S6.** `2.5.741` goes through every gate and deploys to `tdd` on `Default`. A release manager creates `2.5.741-hotfix.1` on `Hotfix` over packages `2.5.741`: `uat` (with `hotfix-justification` and `uat-signoff`), then `prod` (justification, go/no-go, guard, copy, migration, pin), overriding the freeze with a reason. The pin file says `newTag: "2.5.741"` (the package version), and `/_version` starts with `2.5.741`.
- **S7.** Refused: `Default` releases are created only by `svc-codefresh-release` (role `CI Release Publisher`). A second release creator is how the EP20 and EP21 release-number collisions happened.
- **S8.** Dangerous: the older code may read a column the contract migration dropped. Hotfix packages must be at or above every contract migration already applied in the target environment.
- **S9.** The `Hotfix` channel with lifecycle `workorders-hotfix` and the `hotfix-justification` intervention: it skips `tdd` but records who skipped it and why.

</details>

## Expected Outcome

- A one-page promotion map: environment, trigger, gates, approving team.
- The hotfix path written as steps, with the release number and the pinned version distinguished.
- An explanation of why the approver and the deployer must differ in `prod`.

## Discussion

1. What does the weekend freeze protect against that approvals alone do not?
2. Why is the hotfix built by the normal `master` pipeline instead of a special branch build?
3. When Octopus Approvals reaches GA (R20), which step disappears, and what stays?
