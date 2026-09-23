# AI Software Factory Environment Design (Octopus + Argo CD + Codefresh GitOps)

Output of a structured 4-agent design debate (2 rounds: positions, then rebuttals with final votes).
Debaters: **A** Kubernetes/Argo-native, **B** pragmatic minimalist, **C** AI-factory throughput, **D** security/governance.

## Key correction to the starting premise

Argo CD does not build code or run test suites. It keeps a Kubernetes cluster matching what Git says. All four debaters agreed:

- **Integration builds and test suites stay in GitHub Actions.** `.github/workflows/build.yml` already runs unit tests, integration tests against SQL Server, Playwright, Qodana, security scans and the ACR push.
- **Argo CD's job:** one short-lived, isolated environment per agent PR, plus the GitOps sync for test and UAT. That is where the factory is actually bottlenecked.

## The bottleneck being solved

`deploy.yml` sends every deploy through one shared TDD environment (`concurrency: group: deploy-to-tdd-serialized`). Branch deploys cancel each other. So no matter how many agent lanes run, only **one** PR at a time is checked in a deployed environment. The design below changes that to **N at a time**.

## Consensus decisions

| # | Decision | Result | Vote |
|---|---|---|---|
| D1 | Prod runtime | **Azure Container Apps stays for UAT and Prod.** AKS is for non-prod only. Decide on prod AKS at a dated go/no-go after 2 green release cycles. | 4–0 |
| D2 | Where builds and tests run | **GitHub Actions** for build, unit, integration, `[LlmTest]` and the image push to ACR. An Argo CD PostSync Job runs **only** the Playwright acceptance tests against the PR environment. | 4–0 |
| D3 | Promotion authority | **Octopus lifecycles only**, with a human-approved manual intervention before Prod. Codefresh promotion flows stay **off**, so there is one path to prod. | 4–0 |
| D4 | Argo CD install | **Upstream Argo CD 3.x plus the Octopus Argo CD Gateway** on `aks-nonprod`. The Codefresh GitOps Runtime owns the GitOps repo and dashboard. It is installed in "existing Argo CD" mode after a **1-day compatibility spike**, with side-by-side mode as the fallback. | 2–2 split resolved; see below |
| D5 | Per-PR database | **SQL Server container per PR**, with DbUp run as a PreSync hook, on an **on-demand** node pool (spot eviction would kill in-flight tests). Azure SQL for test, UAT and Prod. | 4–0 |
| D6 | Private endpoints | Private endpoints plus Entra-only auth for **UAT and Prod**. Per-PR SQL lives inside the cluster. Non-prod Azure OpenAI goes through APIM with Entra auth. GitHub-hosted runners keep their SQL service containers. | 3–1 |
| — | Identity | **OIDC everywhere.** Delete `AZURE_CREDENTIALS`, `OCTO_API_KEY` and Octopus `az_login_appkey`. Pods use AKS Workload Identity plus External Secrets Operator to Key Vault. | 4–0 |
| — | Agent privileges | Agents hold **no** Argo CD or Octopus credentials and have **zero** Azure RBAC in prod. Argo posts GitHub commit statuses, and agents read status only from GitHub. | 4–0 |

**How D4 was resolved:**
- A and C wanted Codefresh managing Argo CD; B and D wanted upstream Argo CD. Codefresh bundles Argo CD v3.5.2 as of September 2026. Codefresh's system-requirements page says it only attaches to Argo CD 2.12–2.14, but that page looks out of date.
- No source confirms that the Octopus gateway works alongside a Codefresh-managed Argo CD.
- Resolution: use upstream Argo CD as the base so neither vendor pins its version, and prove the two coexist with a spike before relying on it.

## Target structure

```
 Agent lanes (feature-loop) ──PR──► app repo (GitHub)
                                       │
            GitHub Actions (OIDC): build · unit · integration(SQL svc container) · [LlmTest] · image → ACR
                                       │ green + label "preview"
                                       ▼
 aks-nonprod ── Argo CD 3.x ── ApplicationSet (PR generator, webhook) ──► ns wo-pr-<n>
   │   Octopus Argo CD Gateway          UI.Server + Worker + McpServer + SQL pod
   │   Codefresh GitOps Runtime          PreSync: DbUp   PostSync: Playwright
   │   ESO + Workload Identity           default-deny NetworkPolicy, no WI binding, 48h idle TTL
   │                                       │
   │                          Argo notifications → GitHub commit statuses (agents poll GitHub only)
   ▼ merge to master
 Octopus release ──"Update Argo CD Image Tags"──► workorders-gitops (Codefresh-created repo)
   lifecycle: test (AKS, Argo sync) → UAT (ACA) → [HUMAN APPROVAL] → Prod (ACA)
```

**Repositories**
- `bootcamp-palermo-workorders` (existing): code, Dockerfile, tests, and a new `deploy/base/` holding Kustomize or Helm manifests.
- `workorders-gitops` (new, created through Codefresh): `apps/workorders/{base,envs/pr,envs/test,envs/uat}`, the ApplicationSets and the runtime config.
  - Branch protection plus CODEOWNERS. Only Octopus, the Codefresh runtime and the ApplicationSet controller write to it.

**Azure layout**
- **Now:** a non-prod subscription or resource group containing:
  - AKS `aks-nonprod`: a system pool, an on-demand pool for SQL, and a spot pool for app pods.
  - ACR, Key Vault, APIM in front of Azure OpenAI, and Azure SQL for `test`.
- **Existing ACA resource groups stay as they are for UAT and Prod.**
- **Later:** a separate prod subscription.

**Guardrails for PR namespaces (from D)**
- Default-deny NetworkPolicy. No Workload Identity binding. No ESO access to shared secrets.
- ResourceQuota per namespace, and a cap on concurrent preview environments.
- Azure OpenAI reached only through APIM, with per-PR token limits.

## Phased rollout

| Phase | Work |
|---|---|
| 0: Harden (week 1) | Upgrade Octopus to the latest version. Move GitHub→Azure, GitHub→Octopus and Octopus→Azure to OIDC. Delete the static secrets. |
| 1: Platform | AKS non-prod with ACR pull, Key Vault, ESO and Workload Identity. Upstream Argo CD 3.x, the Octopus gateway, and the Codefresh runtime spike. Codefresh creates `workorders-gitops`. |
| 2: Preview environments | App manifests; ApplicationSet PR generator with webhook; SQL pod plus DbUp hook; PostSync Playwright; commit statuses; TTL cleanup; APIM limits. Update the feature-loop skill to wait for `preview/*` statuses. |
| 3: Promotion | New Octopus lifecycle test→UAT→Prod: Argo image-tag step for test, existing ACA step for UAT and Prod, and a manual-intervention step for Prod. Retire the `deploy-to-tdd-serialized` path. |
| 4: Supply chain | SBOM, cosign signing, admission policy, Defender for Containers. |
| 5: Decide | Dated go/no-go on moving Prod to AKS. |

## Tasks the human must do personally

These need a license, a purchase, org-admin rights or an accountable approver, so an agent cannot do them:

1. **Codefresh:** get the account and license through Octopus sales. Free hosted runtimes are gone, and GitOps Cloud pricing is quote-based.
2. **Octopus:** confirm the instance is on the latest version and its license tier includes the Argo CD integration. Create the service account with a GitHub OIDC identity.
3. **Azure:** create or choose the non-prod subscription and resource group. Grant a time-boxed PIM Owner window to the bootstrap identity. Request Azure OpenAI quota if needed.
4. **GitHub (org admin):** approve the Codefresh GitHub App or create the robot account and its PAT, allow creation of `workorders-gitops`, and apply or verify branch protection and CODEOWNERS on that repo.
5. **DNS:** delegate a subdomain such as `pr.<domain>` to Azure DNS, or add the wildcard record.
6. **Name the Prod approvers:** the Octopus team and the CODEOWNERS.
7. **Set budget caps:** maximum concurrent preview environments, node-pool ceiling, and monthly spend.
8. **Approve the pipeline changes** to `.github/workflows/`, `.octopus/` and `Dockerfile` (required by CLAUDE.md).
9. **After bootstrap:** remove PIM eligibility, rotate or revoke bootstrap tokens, and review the Entra and Octopus audit logs.

## Minimum inputs to hand over for fully automated build-out

| # | Item | Form |
|---|---|---|
| 1 | Azure **tenant ID**, **non-prod subscription ID**, **resource group name** and **region** | IDs and names (not secret) |
| 2 | Bootstrap Azure access | PIM-eligible Owner on that resource group for ≤8h, used through `az login` on the provided machine. No client secret. |
| 3 | **Azure OpenAI** endpoint and deployment names, or approval to create them | Names and URL. Keys go into Key Vault, not chat. |
| 4 | **Octopus** instance URL, space name, and a bootstrap API key that expires in ≤24h | URL plus a short-lived key |
| 5 | **Codefresh** account and a runtime-install API key, revoked after install | Short-lived key |
| 6 | **GitHub** robot-account PAT (repo scope, ≤90 days) for the Codefresh runtime and Octopus Git commits, plus org-admin approval of the Codefresh GitHub App | Placed in a Key Vault or GitHub environment secret |
| 7 | **DNS** subdomain for preview environments (e.g. `pr.example.com`) | Name |
| 8 | **Prod approver** names | GitHub and Octopus users |
| 9 | **Caps:** maximum concurrent preview environments and monthly budget | Numbers |
| 10 | **Written approval** to modify pipeline and Octopus files | One sentence |

The provided machine needs: `az`, `kubectl`, `helm`, `argocd`, `octopus` CLI, the Codefresh CLI, `gh`, and the .NET 10 SDK. An agent can install these if the machine allows it.

## Open items to verify during Phase 1

- Which Argo CD versions the Octopus Argo CD Gateway supports. The gateway docs do not list any.
- Whether the Codefresh runtime works in "existing Argo CD" mode (label resource tracking) alongside the Octopus gateway. Covered by the 1-day spike.
- Whether the Codefresh runtime accepts a GitHub App instead of a classic PAT.
- Which Octopus license tier includes the Argo CD integration and ephemeral environments.

## Sources consulted by the debaters

- Octopus Argo CD: https://octopus.com/docs/argo-cd · https://octopus.com/docs/argo-cd/instances · https://octopus.com/blog/argo-cd-in-octopus
- Octopus Argo CD Gateway chart: https://github.com/OctopusDeploy/octopus-argocd-gateway-chart-docs
- Octopus OIDC with GitHub Actions: https://octopus.com/docs/octopus-rest-api/openid-connect/github-actions
- Octopus ephemeral environments: https://octopus.com/docs/projects/ephemeral-environments
- Octopus ACA step: https://octopus.com/integrations/azure/azure-deploy-container-app
- Codefresh runtimes: https://codefresh.io/docs/docs/installation/gitops/ · https://codefresh.io/docs/docs/installation/gitops/argo-with-gitops-side-by-side/ · https://codefresh.io/docs/docs/installation/gitops/runtime-system-requirements/
- Codefresh Git tokens: https://codefresh.io/docs/docs/security/git-tokens/
- Codefresh runtime chart bump to Argo CD 3.5.2: https://github.com/codefresh-io/gitops-runtime-helm/pull/1266
- Argo CD PR generator: https://argo-cd.readthedocs.io/en/latest/operator-manual/applicationset/Generators-Pull-Request/
- ESO + Azure Key Vault: https://external-secrets.io/latest/provider/azure-key-vault/
