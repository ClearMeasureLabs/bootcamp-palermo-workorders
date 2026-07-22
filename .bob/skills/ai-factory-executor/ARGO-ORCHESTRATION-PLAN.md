# Argo Orchestration Plan — Concurrent, Isolated AI Dev Containers

## Goal

Replace the sequential PowerShell orchestrator (`run-factory.ps1` → `container-manager.ps1`)
with **Argo Events + Argo Workflows** on a single-node Kubernetes cluster, so that
multiple GitHub issues ("tasks") are implemented **concurrently** on one machine,
each in a **completely isolated** AI Dev container (network, database, secrets,
repo clone).

Today: one machine, one Docker daemon, issues processed **one at a time** (`foreach` in
`run-factory.ps1`), shared `ai-factory` bridge network, shared `ai-factory-nuget` volume,
host GitHub token + agent key mounted into every container.

Target: one machine running k3s; each labeled issue triggers an Argo Workflow that runs
one agent pod plus a sidecar SQL Server, on its own network policy, with per-task scoped
secrets and its own git clone. N tasks run in parallel bounded only by a concurrency cap.

---

## Diagrams

Full C4 + 4+1 set (source `.puml` + PNGs) in [`argo/diagrams/`](argo/diagrams/README.md).

| C1 — Context | C2 — Container |
|---|---|
| ![C1 Context](argo/diagrams/c4/c1-context.png) | ![C2 Container](argo/diagrams/c4/c2-container.png) |

| C3 — Component | C4 — Code |
|---|---|
| ![C3 Component](argo/diagrams/c4/c3-component.png) | ![C4 Code](argo/diagrams/c4/c4-code.png) |

**4+1 views** — Logical · Process · Development · Physical · Scenarios(+1)

| Logical | Process |
|---|---|
| ![Logical](argo/diagrams/views/logical-view.png) | ![Process](argo/diagrams/views/process-view.png) |

| Development | Physical |
|---|---|
| ![Development](argo/diagrams/views/development-view.png) | ![Physical](argo/diagrams/views/physical-view.png) |

![Scenarios](argo/diagrams/views/scenarios-view.png)

---

## Architecture

```
GitHub issue labeled "AI Factory"
        │  (webhook / repeating poll)
        ▼
Argo Events: EventSource ──► Sensor ──► submits Workflow (params: issue #, title, body, branch, agent)
        │
        ▼
Argo Workflows controller (namespace: ai-factory)
        │  spawns one Workflow per issue, up to `parallelism` at once
        ▼
┌─────────────────────────── Workflow (per issue) ────────────────────────────┐
│  Pod: ai-factory-agent                                                        │
│    - init container: git clone repo @ branch into emptyDir /workspace         │
│    - main container: agent-entrypoint runs Claude/Bob, quality gates, PR      │
│    - sidecar container: mssql (ephemeral, localhost-only, dies with pod)      │
│    - secrets: gh_token + agent key projected from per-run k8s Secret          │
│    - NetworkPolicy: egress to GitHub + Anthropic + DNS only; no pod-to-pod    │
│    - resources: cpu/mem requests+limits; own emptyDir workspace + nuget cache │
└───────────────────────────────────────────────────────────────────────────────┘
```

### Why this satisfies "complete isolation between tasks"
- **Repo clone** — each pod clones into its **own** `emptyDir` `/workspace`; no shared
  host bind-mount (removes the current `-v ../../..:/workspace` collision risk).
- **Database** — a **per-pod** SQL Server sidecar on pod-local `localhost:1433`; two tasks
  can never touch the same DB. Replaces the current shared/DinD approach.
- **Secrets** — a short-lived per-run k8s Secret is projected read-only; no host key files
  (`Initialize-GitHubSecret`, `Initialize-AgentSecret`) reused across tasks.
- **Network** — a default-deny NetworkPolicy blocks pod-to-pod traffic; each task can only
  reach GitHub, Anthropic, and DNS. The current shared `ai-factory` bridge is removed.
- **Compute** — pod resource requests/limits enforce fair sharing; k8s scheduler + Workflow
  `parallelism` bound concurrency instead of a sequential loop.

---

## Reuse vs. new

**Reuse as-is (no logic change):**
- `Dockerfile.agent` and the resulting `ai-factory-agent` image (imported into k3s).
- `agent-entrypoint.ps1` — already reads everything from **env vars + `/run/secrets/*`**,
  which map cleanly to k8s env + projected Secret volumes. Only the DB connection string
  and the "clone the repo" assumption need review (clone moves to an init container).
- `experiment-entrypoint.ps1`, `pr-monitor.ps1` — carry over as alternate entrypoints.

**New (this plan):**
- k3s single-node cluster + Argo Workflows + Argo Events install.
- `WorkflowTemplate` describing the per-issue agent pod (init/main/sidecar).
- Argo Events `EventSource` + `Sensor` for issue discovery → Workflow submission.
- Per-run Secret creation and NetworkPolicy.
- A thin submitter (replaces `run-factory.ps1`) OR let the Sensor submit directly.

---

## Implementation phases

### Phase 0 — Cluster
1. Install k3s (`--disable traefik` if not needed) on the host.
2. Import the agent image: build with existing `Dockerfile.agent`, then
   `k3s ctr images import` (or push to a local registry) so pods pull it without a hub.
3. Create namespace `ai-factory`; install Argo Workflows and Argo Events into it via the
   official manifests pinned to a specific version.
   > Note: this introduces k3s + Argo as new infra. Per CLAUDE.md, build/pipeline/infra
   > changes need approval — treat the manifests as reviewable artifacts, pin all versions.

### Phase 1 — WorkflowTemplate (`argo/workflowtemplate-agent.yaml`)
- Inputs: `issueNumber`, `issueTitle`, `issueBody`, `issueUrl`, `branchName`,
  `repoOrg`, `repoName`, `aiAgent`, `runQualityGates`.
- `initContainers`: `git clone`/checkout branch into shared `emptyDir` `/workspace`.
- `containers[0]` (agent): image `ai-factory-agent`, entrypoint `agent-entrypoint.ps1`,
  env mapped from inputs (same names the entrypoint already reads), `/run/secrets/*`
  from a projected Secret, `ANTHROPIC_API_KEY`/`BOBSHELL_API_KEY` per agent.
- `containers[1]` (sidecar): `mcr.microsoft.com/mssql/server`, `ACCEPT_EULA`, SA password
  from Secret, marked so the pod completes when the agent exits (Argo sidecar semantics).
- `volumes`: `emptyDir` workspace, `emptyDir` (or per-pod PVC) nuget cache.
- `resources`: requests/limits (start from current `--cpus 16 --memory 24g`, tune down
  so several fit on one box; make them template params).
- `activeDeadlineSeconds` = the current 60-min timeout.
- Point the app/integration-test DB connection string at `localhost` (the sidecar).

### Phase 2 — Isolation hardening
- `NetworkPolicy`: default-deny ingress+egress in the namespace; allow-list egress to
  DNS, GitHub, Anthropic (and NuGet/Playwright download hosts) only.
- Per-run Secret: Sensor (or submitter) creates `agent-secret-<issue>` with gh token +
  agent key, `ownerReference`d to the Workflow so it's garbage-collected on completion.
- `securityContext`: non-root (image already uses `bobagent` UID 1001); drop capabilities;
  read-only root FS where the entrypoint allows.
- Decide on DinD: with a real SQL sidecar, `--privileged` DinD should no longer be needed —
  removing it is a large isolation/security win over the current `EnableDocker` path.

### Phase 3 — Trigger (Argo Events)
- **EventSource** — choose based on the answer below:
  - GitHub webhook `issues`/`label` events (real-time), or
  - a `calendar`/`resource` polling source that runs the existing
    `gh issue list --label "AI Factory"` discovery on an interval.
- **Sensor** — filters to open issues labeled `AI Factory` with **no open PR** (port the
  dedup logic from `run-factory.ps1`), then submits the WorkflowTemplate with issue params.
- **Concurrency** — set Workflow/controller `parallelism` (e.g. 3–4) so the box isn't
  overrun; excess issues queue. This is the concurrency the sequential loop lacked.

### Phase 4 — Submitter & observability
- Keep a thin `submit.ps1`/`argo submit` path for manual/whitelist runs (replaces
  `-IssueNumbers`), so the factory can be driven without waiting for a webhook.
- Logs/status via `argo list`, `argo logs`, and the Argo UI (port-forwarded) instead of
  `Get-ContainerLogs`/`Wait-ContainerCompletion`.
- Result summary: a final Workflow step resolves the created PR number (port from
  `run-factory.ps1` step 2) and annotates the Workflow.

### Phase 5 — Cutover
- Run Argo path in parallel with the PowerShell path against a test label
  (e.g. `AI Factory Argo`) to validate concurrency + isolation.
- Verify: two issues run at once, each with its own DB, no cross-talk, PRs open correctly.
- Flip the real `AI Factory` label to the Argo trigger; retire `run-factory.ps1` (keep
  `container-manager.ps1` as a local single-shot fallback).

---

## Deliverables
```
.bob/skills/ai-factory-executor/argo/
  install.ps1                 # k3s + Argo install + image import (versions pinned)
  workflowtemplate-agent.yaml # per-issue agent pod (init + agent + mssql sidecar)
  eventsource.yaml            # GitHub webhook OR polling source
  sensor.yaml                 # filter labeled/no-PR issues → submit workflow
  networkpolicy.yaml          # default-deny + egress allow-list
  rbac.yaml                   # ServiceAccount for the workflow + sensor
  submit.ps1                  # manual/whitelist submission
  README.md                   # run book
```

## Open questions
1. **Trigger latency** — GitHub webhook (needs an ingress/tunnel reachable from GitHub) vs.
   interval polling (no inbound network, simpler on a single box)? Polling is the safer
   default for one machine behind NAT.
2. **Concurrency cap** — how many agents at once given the box's CPU/RAM? Each currently
   asks for 16 CPU / 24 GB; that must drop sharply (e.g. 4 CPU / 8 GB) to fit several.
3. **Image distribution** — `k3s ctr images import` (simplest) vs. a local registry
   (better if the image is rebuilt often).
4. **Approval** — k3s + Argo are new infra/tooling; confirm this is approved per the
   "no infra/pipeline changes without approval" rule before Phase 0.
```
```
