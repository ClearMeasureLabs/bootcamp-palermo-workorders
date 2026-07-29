# deploy/ — Kubernetes manifests for the Argo CD / Codefresh exploration

GitOps source of truth for running the Work Order app on a **throwaway** Kubernetes
cluster (AKS Spot or local kind/k3d). This is exploration scaffolding — it does **not**
touch the production Azure Container Apps deployment, which stays driven by
`.octopus/deployment_process.ocl`.

## Layout

```
deploy/
  base/                 # environment-agnostic manifests
    namespace.yaml
    deployment.yaml     # port 8080, entrypoint from /Dockerfile, /_healthcheck probes
    service.yaml
    ingress.yaml
    secret.example.yaml # template only — NOT applied; real secret created out-of-band
    kustomization.yaml
  overlays/
    dev/
      kustomization.yaml  # sets the image tag Argo/Octopus reconcile
```

## Before it will run

1. Replace `REGISTRY_PLACEHOLDER` in `overlays/dev/kustomization.yaml` (and it flows
   into the Deployment via the kustomize `images` transformer) with your registry,
   e.g. `myacr.azurecr.io`.
2. Build & push the image (the root `Dockerfile` expects published output in `./built/`):
   ```bash
   docker build -t <registry>/workorders:0.1.0 .
   docker push <registry>/workorders:0.1.0
   ```
3. Create the DB secret on the cluster (never in Git):
   ```bash
   kubectl create namespace workorders
   kubectl -n workorders create secret generic workorders-secrets \
     --from-literal=SqlConnectionString='Server=tcp:...;Database=...;User ID=...;Password=...;'
   ```

## Try it locally before wiring Argo

```bash
kubectl apply -k deploy/overlays/dev
kubectl -n workorders rollout status deploy/workorders-server
kubectl -n workorders port-forward svc/workorders-server 8080:80
# browse http://localhost:8080/_healthcheck
```

## Then point Codefresh/Argo CD at it

Create an Argo CD `Application` with:
- **repo**: this repository
- **path**: `deploy/overlays/dev`
- **destination**: the throwaway cluster, namespace `workorders`

Octopus's **Update Argo CD Application Image Tag** step rewrites `newTag` in
`overlays/dev/kustomization.yaml`; Argo CD reconciles the commit onto the cluster;
Octopus's **Wait for Argo CD Applications** step gates the release on health.
