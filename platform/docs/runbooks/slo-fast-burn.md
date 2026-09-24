# SLO fast burn

Response to alert `slo-fast-burn-ui-server-<env>`: `ui-server` is spending its error budget fast
enough to exhaust it in about two days.

Contracts: ADR-D15 (observability and SLOs), ADR-D12 (health endpoints), §7.9, §9 (the alerts go
live in phase 3). Defined in `terraform/environment/monitoring.tf`.

## The alert

| Item | Value |
|---|---|
| SLO | 99.5 % of `ui-server` requests complete without a 5xx over 28 days (error budget 0.5 %) |
| Condition | Burn rate at least 13.44 over the last hour and over the last 5 minutes, with at least 50 requests in the hour. 13.44 = 2 % of the 28-day budget in one hour (0.02 x 672 hours) |
| Excluded | `/alive`, `/health`, `/_healthcheck`, `/_healthcheck/detailed`, `/_version` (probes and deployment checks) |
| Source | `appi-workorders-<env>` (workspace `log-workorders`), evaluated every 5 minutes |
| Severity | 1 in `prod` (page), 3 in `tdd` and `uat` (ticket) |
| Receivers | Action group `ag-workorders-<cluster>-oncall` |
| Resolution | Automatic once the condition is false (auto-mitigation) |

## Roles

| Role | Who | Does |
|---|---|---|
| Responder | Team `SRE On-call` | Acknowledges within 15 minutes (prod), triages, leads the incident |
| Release Manager | Team `Release Managers` | Decides rollback or forward fix; freezes deployments |
| App team | Owners of the app repository | Diagnoses code and dependency faults |

## Preconditions

- Read access to `appi-workorders-<env>` and `log-workorders` (Reader or Monitoring Reader).
- Octopus read access to project `workorders`; Argo CD SSO read access (`role:readonly`).
- For cluster reads beyond Argo CD: PIM `Azure Kubernetes Service RBAC Cluster Admin`
  (`break-glass.md`, path C).

## Steps

1. Acknowledge the alert and open an incident record with the alert time.
2. Confirm the burn with the alert's own query (App Insights `appi-workorders-<env>`, Logs), then
   locate it:

   ```kusto
   let excludedPaths = dynamic(["/alive", "/health", "/_healthcheck", "/_healthcheck/detailed", "/_version"]);
   requests
   | where timestamp > ago(2h)
   | extend path = tostring(parse_url(url).Path)
   | where path !in (excludedPaths)
   | summarize total = count(), failed = countif(toint(resultCode) >= 500) by bin(timestamp, 5m), name
   | extend failureRate = round(100.0 * failed / total, 2)
   | where failed > 0
   | order by timestamp desc, failed desc
   ```

   ```kusto
   exceptions
   | where timestamp > ago(2h)
   | summarize occurrences = count() by type, outerMessage, operation_Name
   | top 10 by occurrences
   ```

   ```kusto
   dependencies
   | where timestamp > ago(2h) and success == false
   | summarize failures = count() by type, target, resultCode
   | top 10 by failures
   ```

3. Correlate with change, newest first:
   - Octopus: the last deployment of `workorders` to `<env>` and its time against the start of
     the burn.
   - Argo CD: the sync history of `workorders-<env>`; any `OutOfSync` or `Degraded` resource.
   - Environment repository: recent merges to `gitops/workorders/envs/<env>/config`.
   - Key Vault and ESO: `kubectl get externalsecret -n workorders-<env>` shows `SecretSynced`.
   - Azure status for SQL, Azure OpenAI and the region.
4. Mitigate:
   - The burn started with a deployment: follow `rollback-and-forward-fix.md` now; diagnose after.
   - A configuration merge: revert the pull request.
   - A dependency is failing (SQL, Azure OpenAI, Key Vault): mitigate the dependency; the LLM
     checks degrade features but should not fail requests; a 5xx storm from one dependency is a
     finding for the app team.
   - Capacity (CPU or memory saturation, restarts): check `kubectl top pods -n workorders-<env>`
     and restarts; scale by pull request to the environment config, never with
     `kubectl scale` (self-heal reverts it).
5. Stop further change: in prod, a Release Manager adds an Octopus deployment freeze for
   `workorders` until the burn stops, except for the fix itself.
6. Communicate status in the incident channel every 30 minutes until resolved.

## Verification

- The alert resolves (auto-mitigation) and stays quiet for 30 minutes.
- The locating query shows the failure rate back under 0.5 % per 5-minute bin.
- Budget left over the 28-day window, recorded in the incident:

  ```kusto
  let excludedPaths = dynamic(["/alive", "/health", "/_healthcheck", "/_healthcheck/detailed", "/_version"]);
  requests
  | where timestamp > ago(28d)
  | extend path = tostring(parse_url(url).Path)
  | where path !in (excludedPaths)
  | summarize total = count(), failed = countif(toint(resultCode) >= 500)
  | extend availability = 1.0 - todouble(failed) / total
  | extend budgetLeft = 1.0 - (todouble(failed) / total) / 0.005
  ```

## Audit evidence

- The alert history of `slo-fast-burn-ui-server-<env>` (Azure Monitor, Alerts) with fired and
  resolved times.
- The incident record: timeline, cause, mitigation, budget left, and the decision on the
  deployment freeze.
- Octopus and Argo CD history of any rollback or fix (`rollback-and-forward-fix.md`).
- A post-incident review within five business days when prod burned more than 10 % of the
  28-day budget, with actions tracked as work items.
