# Handoff — AI Dev Factory on Argo (resume in a k8s-capable environment)

This session built the Argo orchestration + a KEEP_ALIVE serve mode, but **could
not run k3s in the sandbox** (see Blocker). Pick this up in an environment that
can run Kubernetes.

## Status

### Done (in the working tree)
- **Plan + diagrams** — `../ARGO-ORCHESTRATION-PLAN.md`; C4 (c1–c4) + 4+1 views in `diagrams/` (PNGs embedded).
- **Argo manifests** — `workflowtemplate-agent.yaml` (init-clone + agent + mssql sidecar, non-root, 3–4 CPU / 6–8 GB, semaphore = 3), `semaphore-config.yaml`, `networkpolicy.yaml`, `rbac.yaml`, `install.ps1`.
- **Entry point** — `implement-issue.ps1 --issueid N [--agent claude|bob] [--no-watch]`.
- **Deferred trigger** — `eventsource.yaml`, `sensor.yaml`, `workflowtemplate-dispatcher.yaml`, `scripts/dispatch.ps1` (not applied by `install.ps1`).
- **Serve mode** — `Dockerfile.agent` installs `cloudflared`; `agent-entrypoint.ps1` has a `KEEP_ALIVE=true` branch: after the PR it runs the app on `:8080`, opens a Cloudflare quick tunnel, prints `AI_FACTORY_LIVE_URL issue=… pr=… url=https://….trycloudflare.com`, and holds the container open.
- **Pester test** — `tests/AiDevFactory.Tests.ps1` (see Task 5). Runs against Argo; skips itself when no cluster/creds are present.

### Blocked (environmental)
k3d/k3s will not start in the original sandbox: root `cgroup.type = domain threaded`,
so the kernel refuses `memory` controller delegation (ENOTSUP) and kubelet requires
memory cgroup v2. Not fixable from inside that container.

### Remaining
- **Task 4** — wire `KEEP_ALIVE` / `SERVE_PORT` env into `workflowtemplate-agent.yaml` and surface the tunnel URL as a Workflow output (currently the CLI/test greps pod logs for the `AI_FACTORY_LIVE_URL` marker).
- **Task 5** — run `tests/AiDevFactory.Tests.ps1` end-to-end on a real cluster.

## Preflight — confirm the host can run k8s
```bash
stat -fc %T /sys/fs/cgroup          # want: cgroup2fs
cat  /sys/fs/cgroup/cgroup.type     # want: domain   (NOT "domain threaded")
```
If `cgroup.type` is `domain threaded`, k8s won't run there either. A real Linux
host, a VM, or WSL2 with systemd works.

## Bring-up (paste-ready)
```bash
# 1. Tools (installed to ~/bin last time)
mkdir -p ~/bin; export PATH="$HOME/bin:$PATH"
curl -sSL -o ~/bin/kubectl "https://dl.k8s.io/release/$(curl -sL https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"; chmod +x ~/bin/kubectl
curl -sSL -o ~/bin/k3d "https://github.com/k3d-io/k3d/releases/download/v5.7.4/k3d-linux-amd64"; chmod +x ~/bin/k3d
curl -sSL -o /tmp/argo.gz "https://github.com/argoproj/argo-workflows/releases/download/v3.5.11/argo-linux-amd64.gz"; gunzip -f /tmp/argo.gz; chmod +x /tmp/argo; mv /tmp/argo ~/bin/argo
curl -sSL -o ~/bin/cloudflared "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64"; chmod +x ~/bin/cloudflared
pwsh -NoProfile -c 'Install-Module Pester -MinimumVersion 5.5.0 -Force -Scope CurrentUser -SkipPublisherCheck'

# 2. Cluster
k3d cluster create ai-factory --servers 1 --k3s-arg "--disable=traefik@server:0" --wait

# 3. Creds
gh auth login                        # or ensure gh is authed
export ANTHROPIC_API_KEY=...         # or reuse ~/.claude/.credentials.json (claude agent)

# 4. Image  (NOTE: no bobshell tarball in context -> use --agent claude)
cd .bob/skills/ai-factory-executor
docker build -t ai-factory-agent:latest -f Dockerfile.agent .
k3d image import ai-factory-agent:latest -c ai-factory

# 5. Deploy Argo + manifests
pwsh ./argo/install.ps1

# 6. Run the Pester test (creates 3 issues, runs 3 isolated workflows, prints 3 live URLs)
pwsh -c "Invoke-Pester ./argo/tests/AiDevFactory.Tests.ps1 -Output Detailed"
```

## Notes / gotchas
- **k3s NetworkPolicy**: default flannel does not enforce it. For the network-isolation guarantee, create the cluster with a policy-capable CNI (Calico) or accept that `networkpolicy.yaml` is declarative-only on stock k3d.
- **Bob agent**: unavailable until `bobshell-1.0.6.tgz` is placed in the build context. Default to `claude`.
- **Tunnel URL surfacing**: until Task 4 wires a Workflow output, the URL is discovered by grepping pod logs for `AI_FACTORY_LIVE_URL`.
- **Cost/time**: each of the 3 agents makes real LLM calls and runs the full quality gates (PrivateBuild + AcceptanceTests). Budget ~15–30 min/issue.
