# Break-glass

Emergency access that bypasses a normal control of the workorders platform, and how to close it
again. Every path here is time-bound, needs two people and leaves evidence in
`log-workorders`, Entra and Octopus.

Contracts: ADR-D11 (Kyverno break-glass), ADR-D10 and ADR-C10 (locks), ADR-D1 (local accounts
off), §5.2 (identities).

## Roles

| Role | Who | Rights used here |
|---|---|---|
| Responder | Team `SRE On-call` | PIM-eligible `Azure Kubernetes Service RBAC Cluster Admin` and `Key Vault Secrets Officer` on the cluster and environment resource groups (`terraform/foundation`, `breakglass_group_object_id`) |
| Approver | A member of `Release Managers` or `Prod Approvers` other than the responder | Approves the PIM request and the incident record |
| Security owner | CODEOWNERS for `policies/**` | Reviews every exception within one business day |
| Azure Owner | PIM-activated Owner or User Access Administrator | Only person who can lift or restore a `CanNotDelete` lock |

The responder and the approver are always two different people.

## Preconditions

- An incident record exists (`<incident-id>`) with the impact, the reason the normal path fails
  and the planned end time.
- The approver has agreed in the incident record before any activation.
- The normal path was tried first: a release through Octopus, a pull request to the environment
  repository, or a rollback (`rollback-and-forward-fix.md`).
- Break-glass never uses the stored `Azure Runtime Provisioner`, the org-wide GitHub PAT or any
  shared password.

## Paths

| Situation | Path |
|---|---|
| Kyverno denies an image or workload that must run now | [A. Time-bound PolicyException](#a-time-bound-policyexception) |
| Kyverno is down and its fail-closed webhook blocks prod syncs | [B. Kyverno outage](#b-kyverno-outage) |
| Argo CD SSO or Octopus is unavailable and the cluster needs a human | [C. Direct cluster access](#c-direct-cluster-access) |
| An approved change is blocked by a `CanNotDelete` lock | [D. Lift a lock](#d-lift-a-lock) |

### A. Time-bound PolicyException

Kyverno honours exceptions only when the `kyverno` add-on runs with
`features.policyExceptions.enabled: true` and `features.policyExceptions.namespace: kyverno`
(chart defaults: disabled). Confirm both in `argocd/clusters/<cluster>/addons/kyverno.yaml` before
relying on this path.

1. Activate PIM role `Azure Kubernetes Service RBAC Cluster Admin` on `rg-workorders-aks-<cluster>`
   for at most 4 hours. Justification: `<incident-id>`.
2. Sign in to the cluster (Entra ID only; local accounts are disabled):

   ```bash
   az login
   az aks get-credentials --resource-group rg-workorders-aks-<cluster> --name aks-workorders-<cluster> --format azure
   kubelogin convert-kubeconfig --login azurecli
   ```

3. Create the exception for exactly one workload and one policy, expiring within 4 hours.
   `expiresAt` makes Kyverno ignore it afterwards; step 7 deletes it.

   ```yaml
   apiVersion: policies.kyverno.io/v1
   kind: PolicyException
   metadata:
     name: breakglass-<incident-id>
     namespace: kyverno
     labels:
       policies.workorders/break-glass: "true"
   spec:
     policyRefs:
       - kind: ImageValidatingPolicy
         name: verify-release-signatures
     matchConditions:
       - name: one-workload
         expression: >-
           object.metadata.namespace == 'workorders-prod' &&
           object.kind == 'Deployment' &&
           object.metadata.name == 'ui-server'
     expiresAt: "<RFC 3339 UTC time, at most 4 hours ahead>"
     properties:
       reason: "<one line from the incident record>"
       ticket: "<incident-id>"
       approved-by: "<approver>"
   ```

   Other policy names: `disallow-latest-tag`, `require-resources`, `require-probes`,
   `disallow-privileged` (kind `ValidatingPolicy`). An exception exempts the whole object from
   the named policy; a partial exception (`images`, `allowedValues`) is not supported for
   `ImageValidatingPolicy`.

   ```bash
   kubectl apply -f breakglass-<incident-id>.yaml
   ```

4. Let Argo CD retry the sync of `workorders-<env>`, or sync it from the Argo CD UI.
5. Record in the incident: the exception name, the image digest that ran and why it could not be
   signed by `workorders/release`.
6. Replace the workload with a signed release as soon as possible: a normal or Hotfix-channel
   release through Octopus.
7. Delete the exception, even if it has expired:

   ```bash
   kubectl delete policyexception -n kyverno breakglass-<incident-id>
   ```

8. Deactivate the PIM role.

### B. Kyverno outage

In prod the signature and baseline policies fail closed (`failurePolicy: Fail`), but only for
requests in namespaces named `workorders-*`. Platform namespaces keep working, so Argo CD can repair
Kyverno itself.

1. Check the engine:

   ```bash
   kubectl -n kyverno get pods
   kubectl get validatingwebhookconfigurations | grep kyverno
   ```

2. Restore it through Git first: sync Application `kyverno` in Argo CD, or roll back the last
   change to `argocd/clusters/<cluster>/addons/kyverno.yaml` by pull request.
3. Only if Kyverno cannot start and prod must change before it does: with PIM
   `Azure Kubernetes Service RBAC Cluster Admin` active and the approver on the call, delete the
   Kyverno validating webhook configuration that serves the policies (names start with `kyverno-`
   [VERIFY exact names for CEL policies in Kyverno 1.19]). Admission is then unenforced for every
   namespace until Kyverno recreates the configuration on start. Record the time.
4. Verify recovery: pods ready, the webhook configurations exist again, and
   `kubectl get imagevalidatingpolicies,validatingpolicies` shows the policies ready.

### C. Direct cluster access

Use when Argo CD SSO or Octopus is down and a read or a targeted fix is needed. Desired state
still comes from Git: any change made here is reverted by self-heal unless it is also merged.

1. Activate PIM `Azure Kubernetes Service RBAC Cluster Admin` on `rg-workorders-aks-<cluster>`
   (at most 4 hours).
2. Sign in with `az aks get-credentials ... --format azure` and `kubelogin` (path A, step 2).
   There is no admin kubeconfig: `local_account_disabled = true` and `run_command_enabled = false`
   on both clusters.
3. Read first (`kubectl get`, `kubectl logs`, `kubectl describe`). Any write needs the approver's
   agreement in the incident record.
4. For Argo CD itself: local `admin` stays disabled. If SSO is broken for long, re-enable admin by
   a reviewed pull request to `argocd/bootstrap/values-<cluster>.yaml` (`admin.enabled`) and
   disable it again the same way afterwards.
5. Deactivate the PIM role.

### D. Lift a lock

`CanNotDelete` locks sit on `rg-workorders-prod`, `rg-workorders-aks-prod`, the Terraform state
account and the legacy resource groups. They also block deletes of child resources: an AKS node
pool rotation, re-creating a federated credential after a prod cluster rebuild, removing an expired
pre-release database copy.

1. The Azure Owner activates PIM (Owner or User Access Administrator) with the change or incident
   reference.
2. Delete only the lock the change needs:

   ```bash
   az lock delete --name workorders-cannot-delete --resource-group rg-workorders-prod
   ```

3. Run the approved change (for example `env-apply` in `infra-prod`).
4. Restore the lock by re-applying the foundation, never by hand, so the lock matches code:

   ```bash
   terraform -chdir=terraform/foundation plan -var-file=foundation.tfvars -out=foundation.tfplan
   terraform -chdir=terraform/foundation apply foundation.tfplan
   ```

5. Deactivate PIM.

## Verification

- `kubectl get policyexceptions -A -l policies.workorders/break-glass=true` returns nothing.
- `az lock list --resource-group rg-workorders-prod` shows `workorders-cannot-delete`.
- No PIM assignment for the responder is active (Entra ID, PIM, Azure resources, Active
  assignments).
- `kubectl get imagevalidatingpolicies,validatingpolicies -L policies.workorders/mode` shows the
  prod policies with mode `Enforce`.

## Audit evidence

Attach these to the incident record:

- The PIM activation and approval (Entra ID audit log, service "PIM").
- Kubernetes audit events of the responder, from `log-workorders` (resource-specific table
  `AKSAuditAdmin`; column names [VERIFY] against the workspace):

  ```kusto
  AKSAuditAdmin
  | where TimeGenerated between (datetime(<start UTC>) .. datetime(<end UTC>))
  | extend who = tostring(User.username)
  | where who has "<responder UPN>" or tostring(ObjectRef.resource) == "policyexceptions"
  | project TimeGenerated, who, Verb, RequestUri, ResponseStatus
  | order by TimeGenerated asc
  ```

- Kyverno policy reports for the affected namespace
  (`kubectl get policyreports -n workorders-<env> -o yaml`).
- For a lifted lock: the activity-log alert `workorders-lock-deleted` and the foundation apply log.
- The Octopus task log of any deployment made during the window.
- The security owner's review note, within one business day.
