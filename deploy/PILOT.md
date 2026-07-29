# PILOT.md — local, Docker-Desktop-free GitOps pilot

Runbook for the local pilot path described in `arch/arch-argo-05-local-k3s-topology.puml`:
k3s inside WSL 2, no Docker Desktop, images built via WSLC, GitOps reconciliation via a
Codefresh-hosted Argo CD control plane. This is exploration scaffolding per
`deploy/README.md` — production stays on Azure Container Apps until proven.

Reference: `arch/arch-argo-00-overview.md` for the full diagram set. This pilot targets
view 05 (local k3s-in-WSL); view 03 (AKS Spot) is the cloud alternative, not covered here.

## P1 — WSLC pilot (done, 2026-07-29)

Verified on this workstation:

- WSL 2.9.4 installed, WSLC-capable. `wslc.exe` and `container.exe` live in
  `C:\Program Files\WSL`, not on `PATH`.
- Smoke test passed:
  ```powershell
  & "$env:ProgramFiles\WSL\wslc.exe" run --rm hello-world
  ```
- Volume-mount render passed against the CI-pinned image `plantuml/plantuml:1.2026.2`,
  mounting `arch/*.puml`.
- `arch/render-diagrams.ps1` now prefers `docker` → `wslc` → `.tools/plantuml.jar`, in that
  order, so diagram rendering keeps working with Docker Desktop not running (it is installed
  on this workstation but unused — the script's `docker info` guard falls back to `wslc`).
- Ubuntu WSL distro has systemd enabled (PID 1 = systemd) — the k3s prerequisite is met.

Re-run verification any time:

```powershell
& "$env:ProgramFiles\WSL\wslc.exe" run --rm hello-world
& "$env:ProgramFiles\WSL\wslc.exe" run --rm -v "${PWD}\arch:/arch" plantuml/plantuml:1.2026.2 -tpng /arch/arch-argo-05-local-k3s-topology.puml
wsl.exe -d Ubuntu -- ps -p 1 -o comm=
```

## P2 — k3s install in Ubuntu WSL

```powershell
wsl.exe -d Ubuntu -- sh -c "curl -sfL https://get.k3s.io | sudo sh -s - --write-kubeconfig-mode 644"
```

Verify:

```powershell
wsl.exe -d Ubuntu -- sudo k3s kubectl get nodes -o wide
wsl.exe -d Ubuntu -- sudo k3s kubectl get pods -A
```

Node status should read `Ready`; `kube-system` pods (`coredns`, `local-path-provisioner`,
`metrics-server`, `traefik`) should be `Running`.

Reset / teardown:

```powershell
wsl.exe -d Ubuntu -- sudo /usr/local/bin/k3s-uninstall.sh
```

## P3 — App image

Build published output, then build the container image with the root `Dockerfile` via
WSLC (registry required — Argo pulls from a registry, not a local Docker cache, so
`kind`/`k3d`-style local-load shortcuts do not apply here):

```powershell
dotnet publish src/UI/Server -c Release -o ./built
& "$env:ProgramFiles\WSL\wslc.exe" build -t <registry>/workorders:0.1.0 .
& "$env:ProgramFiles\WSL\wslc.exe" push <registry>/workorders:0.1.0
```

`<registry>` is a GHCR or ACR repository reachable from both the build machine and the
k3s node (e.g. `ghcr.io/clearmeasurelabs/workorders` or `myacr.azurecr.io/workorders`).
Authenticate the registry (`wslc login` / `az acr login`) before pushing.

## P4 — Deploy manifests to k3s

```powershell
wsl.exe -d Ubuntu -- sudo k3s kubectl create namespace workorders
wsl.exe -d Ubuntu -- sudo k3s kubectl -n workorders create secret generic workorders-secrets `
  --from-literal=SqlConnectionString='Server=tcp:...;Database=...;User ID=...;Password=...;'
wsl.exe -d Ubuntu -- sudo k3s kubectl apply -k deploy/overlays/dev
wsl.exe -d Ubuntu -- sudo k3s kubectl -n workorders rollout status deploy/workorders-server
wsl.exe -d Ubuntu -- sudo k3s kubectl -n workorders port-forward svc/workorders-server 8080:80
```

Browse `http://localhost:8080/_healthcheck`. Set `REGISTRY_PLACEHOLDER` in
`deploy/overlays/dev/kustomization.yaml` to the registry from P3 first (see
`deploy/README.md`).

## P5 — Codefresh GitOps runtime

Install the Codefresh-hosted Argo CD runtime onto k3s (outbound-only — no inbound port
required, matching `arch-argo-05-local-k3s-topology.puml`'s hosted-control-plane split):

```powershell
wsl.exe -d Ubuntu -- sh -c "curl -sfL https://get.codefresh.io | sudo sh"
wsl.exe -d Ubuntu -- codefresh runtime install --provider k3s
```

Create the Argo CD `Application`, repo = this repository, path = `deploy/overlays/dev`,
destination = the local k3s cluster, namespace `workorders` (same shape as
`deploy/README.md`'s "Then point Codefresh/Argo CD at it" section).

## P6 — Octopus wiring

Replace the ACA rollout step with GitOps steps; DB migration steps are unchanged:

| Octopus step | Today (ACA) | Pilot (Argo/k3s) |
|---|---|---|
| Ensure .NET 10 | keep | keep |
| Run DB migrations (DbUp) | keep | keep — Argo never touches the DB |
| App rollout | `az containerapp update` | **Update Argo CD Application Image Tag** (commits `newTag` in `overlays/dev/kustomization.yaml`) |
| Health gate | implicit | **Wait for Argo CD Applications** (gates on Synced + Healthy) |

Add an Argo CD connection (Codefresh runtime endpoint + API token) in Octopus, then insert
the two steps in place of the container-app update step per
`arch/arch-argo-04-release-flow.puml`.

## Known limits

- WSLC has no `docker compose` equivalent yet — multi-container local stacks are not
  possible; k3s manifests are the only supported local composition.
- WSLC exposes no Docker socket, so `kind` and `k3d` (both docker-in-docker) do not work
  under it — k3s-in-WSL (this pilot) is the supported local-cluster path instead.
- WSLC networking is preview quality — port-forward and DNS behavior may differ from a
  Docker Desktop or cloud cluster; treat P4's port-forward step as a smoke test, not a
  production analog.
- k3s state resets when the WSL VM restarts unless the k3s service is left running;
  systemd (confirmed enabled in P1) restarts it automatically on VM boot, but a full
  `wsl --shutdown` still tears down the running VM and any port-forwards must be
  re-established.
