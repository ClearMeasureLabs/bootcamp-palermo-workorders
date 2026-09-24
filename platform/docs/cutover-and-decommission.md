# Cutover and decommission

Checklists for phases P2–P5 of design §9. Each phase lists its entry conditions, the work, the exit criteria (all required) with the evidence that proves them, and how to reverse it. Bootstrap steps are in [bootstrap.md](bootstrap.md).

Rules for every phase:
- **The legacy path stays live and untouched before P5.** The only earlier change to it is the approved single-migration-owner change inside the P4 maintenance window (R13).
- **Every phase before P5 is reversed by stopping the new path.** Nothing in the legacy path depends on the new one.
- **Evidence lives where the action happened.** Octopus deployments, artifacts and interventions; Argo CD history; environment-repo commits; Azure activity logs. Each checklist item links to one of these.
- **A missed criterion extends the phase.** Criteria are not averaged or traded.

## P1 exit (entry to P2)

- [ ] 10 consecutive master commits where `build-result` and `workorders/release` agree, with equal TRX totals.
- [ ] Each release was created exactly once; a rerun of `workorders/release` is a no-op (`IGNORE_EXISTING: true`).
- [ ] The build of record takes at most 1.2× GitHub `build-linux` plus publish.
- [ ] Images are signed, tag-locked and pass `cosign verify` against the `workorders/release` identity.
- [ ] No Octopus API key is used by any new pipeline.

## P2 TDD on AKS

**Entry.** P1 exit; bootstrap steps 5–9 in progress.

**Work.**
- [ ] `env-plan`, then `env-apply` in `infra-nonprod` (bootstrap step 5).
- [ ] Argo CD token and gateway registration token in `<kv-workorders-platform-nonprod>`; gateway connected (steps 6–7).
- [ ] `gitops/workorders/envs/{tdd,uat}/config` values merged by pull request (step 8).
- [ ] `tdd_auto_deploy = true` (step 9).
- [ ] WI-08 (opt-in destructive reset) merged.
- [ ] Kyverno in Audit mode in nonprod.
- [ ] TDD spike: every [VERIFY] item marked for phase 2 is proven or has a recorded fallback, including:
  - the Argo CD step's action type and whether it matches the full image name with the registry (§7.6, Q1);
  - the read-only gateway account without Trigger sync (E7);
  - two deployments committing pins at the same time (Q4);
  - workload identity for worker script pods (Q2);
  - `DeploymentCreate` for the TDD auto-deploy (Q3);
  - the exact Codefresh OIDC `sub` for `codefresh-release-master`.

**Exit criteria.**

| # | Criterion | Evidence |
|---|---|---|
| 1 | At least 20 consecutive TDD releases, at least 90 % green (legacy baseline 207 of 289, 72 %) | Octopus deployments to `tdd` |
| 2 | Median commit → verified TDD no worse than legacy | Octopus deployment timestamps against the release's `app-commit:`; legacy `deploy.yml` run times |
| 3 | Drill: a bad migration leaves the old version serving with no pin commit | Deployment failed at `migrate-database`; no new commit in `gitops/workorders/envs/tdd`; `/_version` unchanged |
| 4 | Drill: drift self-heals | Argo CD history for `workorders-tdd` shows the revert of a manual change |
| 5 | Drill: redeploy-previous completes in under 15 min | Octopus deployment duration |
| 6 | `platform/tdd` is reported on app commits (only if R16 is approved) | Commit statuses on `ClearMeasureLabs/bootcamp-palermo-workorders` |
| 7 | 14 days of Kyverno audit without false denies | Policy reports in nonprod |
| 8 | `infra-nonprod` uses OIDC and the provisioner secret is retired (R4) | Bootstrap step 10; Entra credential list |

**Reverse.**
1. Set `tdd_auto_deploy = false` and stop deploying to `tdd`. Release creation can continue; it is harmless.
2. Optionally run `env-destroy` in `infra-nonprod` to stop cost. It removes resources inside the resource groups, never the groups, the UAMIs or their role assignments.
3. Legacy TDD is unaffected: it has its own database and path.

## P3 UAT on AKS and the Worker

**Entry.** P2 exit.

**Work.**
- [ ] UAT deployments with `uat-signoff` (team `UAT Approvers`).
- [ ] Worker enabled: `replicas` above 0 in `envs/tdd/config` (P2) and `envs/uat/config`, by pull request.
- [ ] SLO fast-burn alert live (`docs/runbooks/slo-fast-burn.md`).
- [ ] UAT data from WI-07 (seed) or a sanitized copy (Q8).
- [ ] WI-12 merged (no `user.name` metric tag).

**Exit criteria.**

| # | Criterion | Evidence |
|---|---|---|
| 1 | Two UAT cycles approved in Octopus | `uat-signoff` interventions with responsible users |
| 2 | UAT smoke is blocking: an unhealthy `smoke-test` stops the deployment, and `Smoke.FailOnDegraded` for `uat` turns `True` once #9016 closes | Octopus process and variable history |
| 3 | The Worker runs 14 days in UAT with no growth in error or dead-letter queues | Queue depth queries in Log Analytics |
| 4 | Octopus Insights shows lead time | Project Insights |

**Reverse.** Set the UAT Worker to `replicas: 0` by pull request and stop UAT deployments on the new path. Legacy UAT keeps serving; the new UAT database is separate.

## P4 Prod cutover

**Entry.**
- [ ] P3 exit.
- [ ] Azure Owner confirms the `CanNotDelete` locks on `rg-workorders-prod`, `rg-workorders-aks-prod` and the legacy resource groups.
- [ ] WI-01, WI-02, WI-03, WI-05 and WI-08 merged.
- [ ] `env-apply` in `infra-prod` with `azure-oidc-env-lifecycle-prod`; prod bootstrap steps 6–8 done (bootstrap step 11).
- [ ] Kyverno Enforce in prod; impersonation and egress hardening decided.
- [ ] The full cutover rehearsed in UAT, including the database copy (Q10).

**Work.** In a maintenance window, follow the single-migration-owner procedure below, then keep the Worker in prod at `replicas: 0` until product sign-off.

**Exit criteria.**

| # | Criterion | Evidence |
|---|---|---|
| 1 | The rehearsal succeeded | UAT rehearsal record with timings |
| 2 | A PITR drill restored within the agreed RTO | `db-restore-pitr` run in `prod` or `uat`; `docs/runbooks/database-restore-pitr.md` |
| 3 | 14 days of prod SLO within budget | SLO alert history |
| 4 | Rollback to the legacy path remains possible until P5 starts | Legacy Container Apps stopped but intact; reverse procedure rehearsed |

### Single-migration-owner procedure

At any moment exactly one delivery path may run DbUp against a given production database.

| Moment | Legacy prod database | New prod database `<sqldb-workorders-prod>` |
|---|---|---|
| Before the window | Legacy path migrates it | New path owns it; it holds no production data |
| During the window | Frozen; its migration owner is disabled | Receives the copy; the new path migrates it (a no-op) |
| After the window | Kept intact, read by nobody, until P5 | The only production database |

Steps:
1. Announce the window. Freeze legacy prod deployments and confirm no legacy deployment is running.
2. Disable the legacy prod migration owner through the approved change to `app:.github/**` or `app:.octopus/**` (R13). Record the pull request.
3. Start the write freeze: stop the legacy app from accepting writes.
4. Copy the legacy prod database into `<sqldb-workorders-prod>` on `<sql-workorders-prod>` (`az sql db copy` when both are in one subscription, otherwise bacpac export and import, as rehearsed; Q10).
5. Verify the copy: row counts per table and an identical DbUp journal (`SchemaVersions`).
6. In Octopus, deploy to `prod` the release built from the same commit legacy prod runs (match the `app-commit:` line; versions differ, `2.4.<run>` against `2.5.<height>`). The process runs `prod-go-no-go` → `sod-guard` → `read-deployment-secrets` → `db-copy-pre-release` → `migrate-database` (no-op: the journal is complete) → `update-argo-cd-image-tags` → `verify-version` → `smoke-test`.
7. Switch DNS for `<prod-hostname>` to the new Gateway; verify from outside the network.
8. End the write freeze. Leave the legacy app stopped but intact.

**Reverse.**
- **Before step 7:** abort; re-enable the legacy migration owner (revert the step-2 pull request); lift the freezes. The new prod database is discarded.
- **After step 7:** freeze prod deployments in Octopus; start a write freeze; copy the new prod database back to the legacy server; revert the step-2 pull request; switch DNS back; lift the freeze. This works only while every migration applied since cutover is expand-only, so that the legacy build runs on the newer schema (walkthrough 02).

## P5 Decommission the legacy path

**Entry.** 30 days after cutover with a change-failure rate no worse than legacy; approvals for each change below (R13).

**Work, in order.** Each step is reversible until a deletion.
- [ ] Disable `deploy.yml` and the legacy publish jobs (approved `app:.github/**` change).
- [ ] Retire the legacy Octopus project and `app:.octopus/` (approved change).
- [ ] After data retention: remove the locks on the legacy resource groups (Azure Owner), then delete the Container Apps and the legacy resource groups.
- [ ] Delete the `OCTO_API_KEY` and `AZURE_CREDENTIALS` secrets and the legacy `AzureAccount`.
- [ ] Decide the AI Software Factory contract (Q9) and the required-check end state (ADR-C6, R14).
- [ ] Apply WI-06 (installers out of the app's runtime rights).
- [ ] Update `app:docs/` and this repo's docs.

**Exit criteria.**

| # | Criterion | Evidence |
|---|---|---|
| 1 | No consumer of legacy artifacts remains | Registry pull logs; no workflow references `churchbulletin.ui` or `rc-<version>` artifacts |
| 2 | Legacy secrets are deleted | GitHub repository secrets; legacy Octopus space |
| 3 | The docs are updated | Merged pull requests |

**Reverse.** Before any deletion, re-enable the workflows and the legacy project from Git history. After the Container Apps and legacy resource groups are deleted, no rollback to the legacy path exists; that is why the 30-day gate and the disable-then-delete order come first.
