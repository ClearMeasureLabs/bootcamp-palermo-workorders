# Lab 19: Ship a Schema or Configuration Change Safely (Expand/Contract)

**Curriculum Section:** Section 05 (Team/Process Design - Database DevOps) and Section 06 (Operate/Execute)
**Estimated Time:** 50 minutes
**Type:** Build + Analyze
**Builds on:** Lab 04 (writing a migration), Lab 18 (follow a commit)

---

## Objective

Plan a database change and a configuration change so that every environment keeps working at every moment: during the rollout, when the next environment still runs the old release, and after a rollback. Practise the expand/contract pattern for the schema (DbUp) and for configuration (Kustomize components, ADR-D6).

## Background

Four platform facts make expand/contract mandatory:

1. **Migrations run before the pin commit.** Octopus step `migrate-database` runs DbUp `update` on the environment's worker (`k8s-<env>`) before `update-argo-cd-image-tags` commits the new tags. For the duration of the rollout the new schema serves the old pods: the rolling update (`maxUnavailable: 0`) keeps an old pod until a new one is ready.
2. **Migrations are forward-only.** DbUp never runs down-scripts. Rollback is Octopus "redeploy previous release": the older package's migration step finds nothing new to run, and the older images start against the newer schema.
3. **Environments lag each other.** `tdd` gets every release automatically; `uat` and `prod` get it days later, after approval. A schema can be two releases ahead of the code in another environment's rollback target.
4. **A shared base changes every environment at once.** A merged change to `gitops/workorders/base/` reaches `tdd`, `uat` and `prod` together, outside the Octopus lifecycle. Changes that must roll through the environments go in a component instead (ADR-D6).

The rule that follows: **every schema and every configuration must work with the release that is running and with the release a rollback would restore.**

## Part A: schema change (expand, migrate, contract)

Example: replace `WorkOrder.RoomNumber` with a more general `Location` column. A single-step rename would break the running release the moment `migrate-database` finishes. Split it across releases:

| Release | Migration | Code | Safe because |
|---|---|---|---|
| A (expand) | `032_AddLocationToWorkOrder.sql`: add `Location` as a nullable column with the same type as `RoomNumber` (see `029_ExtendWorkOrderRoomNumberLength.sql`), then copy `RoomNumber` into it | Writes both columns, reads `RoomNumber` | The old release ignores the new column |
| B (migrate) | none, or a re-copy of rows written by older releases | Reads `Location` (falls back to `RoomNumber`), writes both | Release A and B both keep both columns filled |
| C (contract) | `03x_DropRoomNumberFromWorkOrder.sql` | Reads and writes `Location` only | Only after B is in `prod` and no rollback target below B remains |

Steps:

### Step 1: Write the expand migration

```powershell
ls src/Database/scripts/Update/
```

The highest number is `031`, so the expand script is `032_AddLocationToWorkOrder.sql`. Use TABS for indentation. The migration only adds and copies; it never renames or drops.

### Step 2: Run it locally

```powershell
.\PrivateBuild.ps1
```

The private build runs DbUp against the local database and runs the tests, exactly as in Lab 04.

### Step 3: Predict the platform's behaviour

After the merge, release A (`2.5.<n>`) reaches `tdd` automatically. Answer before looking:
- Which Octopus step applies `032`, on which worker pool, as which database user?
- If `032` fails in `tdd`, what does the environment repo contain afterwards, and which version keeps serving?
- Release A is in `prod`. Release C, which drops `RoomNumber`, is in `tdd` only. A release manager redeploys a release older than A to `uat`. Does anything break? What if C had reached `uat` first?

### Step 4: Decide when the contract may ship

Release C may enter a lifecycle only when:
- release B (or later) is running in `prod`;
- no environment would roll back below B (Octopus shows the previous successful release per environment);
- no `Hotfix` release built from a package older than B is planned.

In `prod`, step `db-copy-pre-release` copies the database to `#{Sql.Database}-pre-<release>` (kept 14 days) before `migrate-database`, and PITR covers the rest (`docs/runbooks/database-restore-pitr.md`). Neither replaces expand/contract: restoring a database loses the writes made since.

## Part B: configuration change (components, ADR-D6)

Where configuration lives:

| What | Where | Changed by |
|---|---|---|
| Shared manifests (probes, resources, NetworkPolicies) | `gitops/workorders/base/` | Pull request; reaches all environments on merge |
| Per-environment values (`workorders-config` literals, client IDs, hostnames, Worker replicas) | `gitops/workorders/envs/<env>/config/` | Pull request per environment |
| Roll-through changes to shared manifests | `gitops/workorders/components/<change>/` enabled per environment | Pull requests in `tdd` → `uat` → `prod` order, then folded into `base/` |
| Secrets | Key Vault → ESO → Secret `workorders-app` | Security owners; never Git |

Configuration follows expand/contract too: **new configuration is added to every environment before the release that needs it reaches that environment**, and removed only after no running or rollback release reads it.

Example: switch the `ui-server` readiness probe from `/alive` to `/ready` once work item WI-01 adds the database-only `/ready` endpoint.

### Step 5: Expand the code first

The WI-01 release must be running in an environment before its probe points at `/ready`; otherwise new pods never become ready and the rollout stalls. Record which release introduced `/ready`.

### Step 6: Add the component

`gitops/workorders/components/ready-probe/kustomization.yaml` (illustrative):

```yaml
apiVersion: kustomize.config.k8s.io/v1alpha1
kind: Component
patches:
  - target:
      kind: Deployment
      name: ui-server
    patch: |-
      - op: replace
        path: /spec/template/spec/containers/0/readinessProbe/httpGet/path
        value: /ready
```

A component is inert until an overlay references it.

### Step 7: Enable it one environment at a time

1. Pull request 1 adds `components: [../../../components/ready-probe]` to `gitops/workorders/envs/tdd/config/kustomization.yaml`. After merge, Argo CD syncs `workorders-tdd` only; no Octopus release is involved. Watch the next TDD deployment's verification.
2. Pull request 2 does the same in `envs/uat/config`, after the WI-01 release is in `uat`.
3. Pull request 3 does the same in `envs/prod/config`, after the WI-01 release is in `prod`.

### Step 8: Fold into the base

One pull request changes the probe in `gitops/workorders/base/ui-server.yaml` and removes the three component references. Render every environment before and after:

```bash
# on main, before the fold
for env in tdd uat prod; do kustomize build gitops/workorders/envs/$env > /tmp/$env.before.yaml; done
# on the fold branch
for env in tdd uat prod; do kustomize build gitops/workorders/envs/$env > /tmp/$env.after.yaml; done
for env in tdd uat prod; do diff -u /tmp/$env.before.yaml /tmp/$env.after.yaml && echo "$env: no change"; done
```

The rendered output must be identical to the output before the fold, so the fold changes nothing at runtime. It still needs platform-owner review (CODEOWNERS on `gitops/workorders/base/`). A base change that *does* alter runtime behaviour needs the `all-environments` label as well.

### Step 9: Know the contract hazard

Once `prod` probes `/ready`, redeploying a release older than WI-01 to `prod` would leave new pods unready. The rollback target is now WI-01 or later, unless the configuration is reverted first. Write this into the release notes of the pull request that enables the component.

## Offline variant: predict every handoff

Use only the staged files. Fill in the predictions, then check.

| # | Question | Prediction | Check in |
|---|---|---|---|
| P1 | Name of the expand script and the directory that holds it | | `app:src/Database/scripts/Update/` |
| P2 | Octopus step, worker pool, container image and database user that apply it in `uat` | | `.octopus/workorders/deployment_process.ocl` (`migrate-database`); `WorkOrders Environment` (`Sql.MigratorUser`) |
| P3 | The Key Vault secret the migrator password comes from, and the step that reads it | | `contracts/platform-contracts.yaml` (`keyVault`); `read-deployment-secrets` |
| P4 | What `gitops/workorders/envs/tdd/kustomization.yaml` contains after a failed `032` in `tdd` | | ADR-C2 |
| P5 | Which steps run for a rollback ("redeploy previous release"), and what `migrate-database` does in it | | §3.2 step 7 |
| P6 | Which Argo CD Applications sync after pull request 1 of Step 7, and which after the fold | | `argocd/clusters/*/apps/*.yaml` paths |
| P7 | Which CODEOWNERS entry and which label apply to a `base/` pull request that changes behaviour | | `CODEOWNERS`; ADR-D6 |
| P8 | Which checks in `codefresh/env-checks` would reject a component that sets `images:` or references `components/bluegreen` | | `scripts/checks/consistency.sh` (C17) |
| P9 | The earliest release a `prod` rollback may target after Step 7.3 | | Step 9 |

<details>
<summary>Answer key</summary>

- **P1.** `032_AddLocationToWorkOrder.sql` in `src/Database/scripts/Update/` (the highest existing number is `031`).
- **P2.** `migrate-database` on `#{WorkerPool}` = `k8s-uat`, in container `#{StepImage.CiDotnet}` (`<acr-name>.azurecr.io/platform/ci-dotnet:<ci-image-version>`), as `workorders_migrator` (until WI-05 moves it to workload identity).
- **P3.** `workorders-sql-migrator-password` in `<kv-workorders-uat>`, read by `read-deployment-secrets` with `azure-oidc-deploy-uat`.
- **P4.** Unchanged: the deployment stops before `update-argo-cd-image-tags`, so no commit is made and the old version keeps serving.
- **P5.** The full process for the older release. `migrate-database` runs DbUp `update`, finds every script already journaled and does nothing; the pin commit writes the older tags; Argo CD rolls back the pods.
- **P6.** After pull request 1: only `workorders-tdd`. After the fold: all three reconcile, but nothing changes because the rendered output is identical.
- **P7.** `/gitops/workorders/base/ @<org>/platform-owners`, plus the `all-environments` label when runtime behaviour changes.
- **P8.** Consistency check C17: config overlays must not set `images:` (only the pin file may) and must not reference `bluegreen` (ADR-C3). C08 guards the pin file's exact shape.
- **P9.** The WI-01 release, or a later one, unless the probe change is reverted first.

</details>

## Expected Outcome

- A three-release plan for a rename, with the gate for the contract release written down.
- A component-based rollout plan for a shared manifest change, and the zero-diff fold.
- The habit of asking "what runs against this schema or configuration if a rollback happens now?"

## Discussion

1. Why does the platform run migrations in Octopus instead of an Argo CD PreSync hook (ADR-C2, E12)?
2. What would break if the readiness switch were merged straight into `base/`?
3. Which environments would a hotfix release built from an older package reach, and what does that mean for contract migrations (Lab 20)?
