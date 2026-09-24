# Rollback and forward fix

How to recover from a bad release of `workorders` in `tdd`, `uat` or `prod`: redeploy the previous
release (rollback), or ship a corrected release (forward fix). Octopus owns both; Argo CD only
applies the result.

Contracts: ADR-D4 and ADR-D5 (promotion writer, sync policy and rollback), ADR-C2 (forward-only
migrations), ADR-D13 (human gates), §7.2 (channels, steps), §7.6 (pin files).

## Roles

| Role | Who | Decides or does |
|---|---|---|
| Incident lead | Team `SRE On-call` | Detects, gathers evidence, runs the Octopus redeploy |
| Release Manager | Team `Release Managers` | Chooses rollback or forward fix; creates Hotfix-channel releases |
| Prod approver | Team `Prod Approvers`, not the person who started the deployment | Answers `prod-go-no-go`; `sod-guard` fails otherwise |
| App team | Owners of `ClearMeasureLabs/bootcamp-palermo-workorders` | Writes the fix; judges schema compatibility |

## Rules

- Roll back only by redeploying an earlier release in Octopus. The redeploy writes the earlier
  image tag into `gitops/workorders/envs/<env>/kustomization.yaml`, and Argo CD syncs it.
- Never run `argocd app rollback`: Argo CD cannot roll back an application with automated sync
  (E40), and a manual sync away from Git is reverted by self-heal.
- Never edit `images[].newTag` in a pin file by hand; only Octopus writes it (§6.1, bot-path
  audit). The one exception is an Octopus outage, handled as a break-glass change by pull request
  with approval (`break-glass.md`).
- Migrations are forward-only (DbUp). An older release runs against the newer schema. A rollback
  is safe only when the bad release's migrations were additive (expand). When they removed or
  renamed something the older code uses (contract), fix forward or restore
  (`database-restore-pitr.md`).

## Preconditions

- The incident record states the environment, the bad release number and the first bad time.
- The previous good release is known: Octopus, project `workorders`, the environment's
  deployment history.
- The bad release's migrations are known: build information, or `src/Database/scripts/Update/`
  in the release commit.

## Decide

| Question | Yes | No |
|---|---|---|
| Did the bad release add only additive migrations (or none)? | Rollback is possible | Forward fix or restore |
| Is the fault in configuration (`gitops/workorders/envs/<env>/config`) rather than the image? | Revert that pull request; no Octopus action | Continue |
| Is data damaged? | `database-restore-pitr.md` first, then continue here | Continue |
| Can a fix be merged and released within the error budget (`slo-fast-burn.md`)? | Forward fix | Rollback now, fix forward later |

## Steps: rollback

1. In Octopus, open project `workorders`, environment `<env>`, and select the last release that
   was healthy there.
2. Choose Redeploy. The full process runs again:
   - in prod, `prod-go-no-go`, `sod-guard` and `db-copy-pre-release`;
   - `read-deployment-secrets` and `migrate-database`; DbUp finds every script of the older
     package already in `SchemaVersions` and applies nothing;
   - `update-argo-cd-image-tags` (a direct commit of the older tag);
   - `verify-version` and `smoke-test`.
3. If `prod-weekend-freeze` is active, a Release Manager overrides it with the reason
   `<incident-id>`.
4. Watch Argo CD Application `workorders-<env>` reach Synced and Healthy at the new pin commit.
   Octopus verifies the same ("Argo CD Application is healthy", 900 s).
5. Keep the bad release out of the environment: raise a Prod-freeze, or tell approvers not to
   promote it, until the fix exists.

## Steps: forward fix

1. The app team merges the fix to `master` through the normal pull request and the
   `build-result` check. Codefresh `workorders/release` builds, signs and creates the next
   release on channel `Default`, which reaches `tdd` automatically.
2. Normal path: promote through `uat` (with `uat-signoff`) to `prod`.
3. Urgent path, skipping `tdd`: a Release Manager creates release `<package-version>-hotfix.<n>`
   on channel `Hotfix` with the same package versions. The Hotfix lifecycle goes to `uat`, then
   `prod`; step `hotfix-justification` records the reason. The packages still come only from
   `workorders/release`, so Kyverno admits the images in prod.
4. Deploy to `prod` with `prod-go-no-go` answered by a Prod approver.

## Verification

- Octopus: the deployment finished and `verify-version` saw the expected version at
  `#{App.BaseUrl}/_version`.
- Argo CD: `workorders-<env>` is Synced and Healthy at the commit that Octopus wrote.
- `smoke-test` passed (`/_healthcheck` Healthy, or Degraded where `Smoke.FailOnDegraded` is
  `False`).
- The fast-burn SLO alert has resolved; the burn-rate query in `slo-fast-burn.md` is below
  13.44 for both windows.
- Kyverno reports no new failures in `workorders-<env>`:
  `kubectl get policyreports -n workorders-<env>`.

## Audit evidence

- The Octopus deployment of the rollback or fix, with the manual-intervention answers
  (`prod-go-no-go`, `hotfix-justification`) and the freeze override reason.
- The pin commit in the environment repository:
  `git log -p -- gitops/workorders/envs/<env>/kustomization.yaml`.
- The Argo CD sync history of `workorders-<env>`.
- For a forward fix: the app pull request, the Codefresh build URL (in the Octopus build
  information) and the release notes line `app-commit: <sha>`.
- The incident record with the decision (rollback or forward fix) and the schema assessment.
