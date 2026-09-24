# Credential rotation

Rotation of every stored credential of the workorders platform: the stored provisioner secret,
the GitHub PAT, the ACR tokens, the SQL passwords, the Argo CD token and the other bootstrap and
application secrets. Federated identities (Octopus OIDC accounts, workload identity, keyless
signing) have no stored secret and are not rotated.

Contracts: §5.2 (identity inventory), §5.3 (stored credentials), §7.8 (Key Vault names),
ADR-C10 and R3 to R5 (recommendations to the user), ADR-D9, ADR-D14.

## Roles

| Role | Who | Rights |
|---|---|---|
| Rotator | Team `Platform Engineers` or `SRE On-call` | PIM-eligible `Key Vault Secrets Officer` on the environment and cluster resource groups; Space Manager in `<octopus-space>`; Codefresh account admin for integrations |
| Entra administrator | §5.2 | Application credentials of the stored provisioner and of `<argocd-sso-app>` |
| Org owner of `clearmeasure-aisf-sample-apps` | GitHub | Fine-grained tokens, GitHub Apps |
| Reviewer | A second person from the rotator's team | Confirms the old credential is revoked |

## Rules for every rotation

- Create the new credential, switch every consumer, verify, then revoke the old one. Never revoke
  first.
- Secret values never go into tickets, chat, shell history or command arguments. Write Key Vault
  secrets from a file that is deleted afterwards:

  ```bash
  az keyvault secret set --vault-name <vault> --name <secret-name> --file ./value.txt --encoding utf-8
  shred -u ./value.txt
  ```

- Activate PIM `Key Vault Secrets Officer` only for the vault's resource group and only for the
  rotation.
- Expiry of every new credential: 90 days or less.
- Terraform never reverts a rotated Key Vault secret: `terraform/environment` writes secrets once,
  with write-only values (`value_wo_version = 1`).

## Schedule

| Credential | Stored in | Cadence | Section |
|---|---|---|---|
| `Azure Runtime Provisioner` client secret | Octopus account `Azure Runtime Provisioner`; Codefresh context `azure-runtime-provisioner` | 90 days, until retired at the phase-2 exit (R4) | [1](#1-stored-provisioner-secret) |
| GitHub fine-grained PAT, org `clearmeasure-aisf-sample-apps` | Octopus Git credential `GitHub clearmeasure-aisf-sample-apps`; Octopus variable set `GitHub AISF Sample Apps`; Codefresh Git integration `github-aisf-sample-apps`; Codefresh context `github-aisf-sample-apps-token` | 90 days (R3) | [2](#2-github-pat) |
| ACR tokens `cf-workorders-release`, `cf-workorders-preview`, `cf-platform-ci` | Codefresh registry integrations `acr-workorders-release`, `acr-workorders-preview`, `acr-platform-ci` | 90 days (R10) | [3](#3-acr-tokens) |
| SQL passwords `workorders_migrator` (all environments), `workorders_acceptance` (`tdd`) | `workorders-sql-migrator-password`, `workorders-sql-acceptance-password` in `<kv-workorders-{env}>` | Monthly, runbook `rotate-sql-passwords` | [4](#4-sql-passwords) |
| Argo CD account `octopus` API token | `argocd-octopus-gateway-token` in `<kv-workorders-platform-{cluster}>` | 90 days | [5](#5-argo-cd-token) |
| Octopus gateway registration token (`svc-argocd-gateway`) | `octopus-gateway-registration-token` in the platform vault | 90 days (Q13) and on every gateway reinstall | [6](#6-other-platform-secrets) |
| Argo CD repository read credential | `argocd-repo-read-credential` in the platform vault | 90 days (token) or yearly (GitHub App key) | [6](#6-other-platform-secrets) |
| Argo CD SSO client secret (only without federation, Q15) | `argocd-sso-client-secret` in the platform vault | 90 days | [6](#6-other-platform-secrets) |
| Octopus worker registration token | Octopus sensitive variable `Octopus.WorkerRegistrationToken` | Per worker install; never reused | [6](#6-other-platform-secrets) |
| Azure OpenAI key, API validation key | `workorders-ai-openai-apikey`, `workorders-api-validation-key` in `<kv-workorders-{env}>` | 90 days, and once after the first `env-apply` for the OpenAI key | [7](#7-application-secrets) |

## Preconditions

- A change record `<change-id>`; for prod, outside `prod-weekend-freeze`.
- The rotator knows every consumer of the credential (Schedule table). A consumer missing from the
  table is a finding for the security owners.

## Steps

### 1. Stored provisioner secret

The user chose to store this secret in Octopus and Codefresh. The design recommends retiring it at
the phase-2 exit in favour of `azure-oidc-env-lifecycle-nonprod` (R4); these steps apply until then.

1. The Entra administrator adds a new client secret to the provisioner's app registration with
   an expiry of 90 days or less, keeping the old one.
2. In Octopus, update account `Azure Runtime Provisioner` (Infrastructure, Accounts) with the new
   secret, and set `Provisioner.SecretExpiresOn` (project `workorders-infrastructure`) to the new
   expiry date.
3. Run runbook `provisioner-credential-check` in `infra-nonprod`; it must pass its `az login` smoke
   test.
4. Codefresh context `azure-runtime-provisioner` is attached to no pipeline (R5). Recommendation to
   the user: delete it instead of updating it. If it is kept, update it in the same change.
5. The Entra administrator deletes the old client secret.

### 2. GitHub PAT

1. The org owner creates a fine-grained token with the same permissions (contents read and write)
   and a 90-day expiry. Recommendation to the user (R3): restrict it to
   `basic-environment-octopus-codefresh` and issue it to the `platform-bots` machine user, or move
   to a GitHub App.
2. Update, in this order:
   - Octopus Git credential `GitHub clearmeasure-aisf-sample-apps`, then test the version-control
     connection of projects `workorders` and `workorders-infrastructure`.
   - Codefresh Git integration `github-aisf-sample-apps`, then push an empty branch to the
     environment repository and confirm `platform-env/env-checks` triggers.
   - Octopus variable set `GitHub AISF Sample Apps` (`GITHUB_TOKEN`) and Codefresh context
     `github-aisf-sample-apps-token`, if they are still kept (R5 recommends deleting both).
3. Run one Octopus deployment to `tdd`; `update-argo-cd-image-tags` must commit the pin.
4. The org owner revokes the old token.

### 3. ACR tokens

Each token has two password slots, so a rotation never interrupts pushes.

1. Generate the unused slot with an expiry (the command prints the password once; run it in a
   private terminal and paste the value straight into Codefresh):

   ```bash
   az acr token credential generate --registry <acr-name> --name cf-workorders-release --password2 --expiration-in-days 90
   ```

2. Update Codefresh registry integration `acr-workorders-release` with the new password.
3. Verify: the next `workorders/release` build pushes and locks its tags (for `cf-platform-ci`,
   run `workorders/ci-image`; for `cf-workorders-preview`, a labelled preview build in phase 6).
4. Delete the old slot:

   ```bash
   az acr token credential delete --registry <acr-name> --name cf-workorders-release --password1
   ```

5. Repeat for `cf-workorders-preview` (`acr-workorders-preview`) and `cf-platform-ci`
   (`acr-platform-ci`), alternating slots on each rotation.

### 4. SQL passwords

Runbook `rotate-sql-passwords` (project `workorders-infrastructure`, monthly trigger) generates a
new password, runs `ALTER USER`, writes Key Vault and verifies a login. Manual procedure when the
runbook fails:

1. Activate PIM `Key Vault Secrets Officer` on `rg-workorders-<env>`; sign in to
   `<sql-workorders-{env}>` with Entra ID as a member of `<sql-admins-{class}>`.
2. Generate a password of at least 32 characters into `./value.txt`.
3. Change the contained user without echoing the password, for example with `sqlcmd -i` reading a
   script generated in the same private shell:
   `ALTER USER [workorders_migrator] WITH PASSWORD = '<generated>';`
4. Write Key Vault secret `workorders-sql-migrator-password` from the file (rules above).
5. Verify: redeploy the current release to `<env>`; `read-deployment-secrets` and
   `migrate-database` succeed. In `tdd` also run `run-acceptance-tests` for
   `workorders_acceptance`.
6. Both passwords disappear after WI-05 (workload identity for DbUp).

### 5. Argo CD token

Account `octopus` is read-only (`applications get`, `logs get`, `clusters get`). Token IDs carry
the date so the old token can be deleted by ID.

1. Sign in to `argocd-<cluster>` through SSO as a platform owner.
2. Create the new token with a 90-day expiry, written straight to a file:

   ```bash
   argocd account generate-token --account octopus --id octopus-$(date +%Y%m%d) --expires-in 2160h > ./value.txt
   ```

3. Activate PIM `Key Vault Secrets Officer` on `rg-workorders-aks-<cluster>` and write
   `argocd-octopus-gateway-token` into `<kv-workorders-platform-{cluster}>` from the file.
4. ExternalSecret `argocd-octopus-token` refreshes within one hour; to refresh at once:

   ```bash
   kubectl annotate externalsecret -n octopus-argocd-gateway argocd-octopus-token force-sync=$(date +%s) --overwrite
   ```

5. Verify in Octopus that instance `argocd-<cluster>` reports healthy and its Applications show
   live status.
6. Delete the old token:

   ```bash
   argocd account get --account octopus
   argocd account delete-token --account octopus <old-token-id>
   ```

### 6. Other platform secrets

- **Gateway registration token**: create a new token for service account `svc-argocd-gateway` in
  Octopus, write `octopus-gateway-registration-token`, force-sync ExternalSecret
  `octopus-gateway-registration`, confirm the gateway is still registered, then revoke the old
  token. Whether the gateway uses the token only at registration is [VERIFY] (Q13).
- **Repository read credential**: create the new read-only token (or a new GitHub App private
  key), write `argocd-repo-read-credential` as the JSON object described in
  `argocd/clusters/<cluster>/platform-secrets.yaml`, force-sync ExternalSecret
  `argocd-repo-creds` in namespace `argocd`, confirm Argo CD shows the repository as connected,
  then revoke the old credential. Never use the stored org-wide PAT here (§5.1 TB7).
- **Argo CD SSO client secret**: only when workload identity federation is not used (Q15). The
  Entra administrator adds a secret to `<argocd-sso-app>`; write `argocd-sso-client-secret`;
  confirm sign-in; delete the old secret.
- **Octopus worker registration token**: used once per install and ends when it expires. To
  re-register a worker, generate a new token, enter it as `Octopus.WorkerRegistrationToken`, and
  run `env-apply` with a replace of `helm_release.octopus_worker["<env>"]`. The token stays out
  of Terraform state (ephemeral variable, write-only argument).

### 7. Application secrets

`workorders-ai-openai-apikey` is created by `env-apply` with a non-key value; set the real key
once after the first `env-apply` and on each rotation. `workorders-api-validation-key` starts as a
random value from `env-apply`.

1. Write the new value from a file into `<kv-workorders-{env}>` (rules above).
2. ExternalSecret `workorders-app` refreshes within one hour; force it with the annotation shown
   in section 5 (namespace `workorders-<env>`).
3. Pods read these values at start. Restart through Git, never with `kubectl rollout restart`
   (self-heal would revert the annotation): merge a pull request that bumps a pod-template
   annotation in `gitops/workorders/envs/<env>/config`.
4. Verify: `smoke-test` of the next deployment, or `/_healthcheck` showing the LLM check healthy.
5. For the OpenAI key, revoke the old key in Azure OpenAI after verification. Keep separate
   low-budget keys for CI and TDD (R15).

## Verification

- Every consumer in the Schedule table works with the new credential (the check named in each
  section).
- The old credential is revoked or deleted; the reviewer confirms it in the change record.
- New expiry dates are recorded (`Provisioner.SecretExpiresOn`, the change record for tokens).

## Audit evidence

- Key Vault audit events of the rotation (who wrote which secret, never the value):

  ```kusto
  AzureDiagnostics
  | where ResourceProvider == "MICROSOFT.KEYVAULT" and OperationName == "SecretSet"
  | where TimeGenerated > ago(1d)
  | project TimeGenerated, Resource, id_s, identity_claim_upn_s, CallerIPAddress
  ```

- Entra audit log entries for application credential changes (provisioner, `<argocd-sso-app>`).
- The Azure activity log for ACR token credential operations.
- The Octopus audit log for account, Git credential and variable changes.
- The GitHub organization audit log for token creation and revocation.
- The PIM activation records of the rotator.
