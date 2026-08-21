# Deploy workflow — GitHub Environment variables

`.github/workflows/deploy.yml` prints `Container App URL: https://…` after Octopus await by calling `az containerapp show`. That step skips (with a warning) when the app name or resource-group variable is empty.

## Required per environment

| GitHub Environment | Variable | Expected value | Notes |
|---|---|---|---|
| TDD | `CONTAINER_APP_NAME` | `ui-gh` | GitHub Actions Container App (not Octopus `ui`) |
| TDD | `TDD_RESOURCE_GROUP_NAME` | `bootcamp-tdd` | Used by TDD FQDN / health steps |
| UAT | `CONTAINER_APP_NAME` | `ui-gh` | Same app name pattern as TDD |
| UAT | `UAT_RESOURCE_GROUP_NAME` | `bootcamp-uat` | Matches Octopus `ResourceGroupName` |
| Prod | `CONTAINER_APP_NAME` | `ui-gh` | Same app name pattern as TDD |
| Prod | `PROD_RESOURCE_GROUP_NAME` | `bootcamp-prod` | Matches Octopus `ResourceGroupName` |

Values align with Octopus `container_app_name=ui-gh` and `ResourceGroupName=bootcamp-{env}`.

## Symptoms when missing

UAT/Prod job logs show:

- `Skipping UAT Container App URL: CONTAINER_APP_NAME or UAT_RESOURCE_GROUP_NAME is empty.`
- `Skipping Prod Container App URL: CONTAINER_APP_NAME or PROD_RESOURCE_GROUP_NAME is empty.`

## How to set

```bash
gh variable set CONTAINER_APP_NAME --env UAT --body ui-gh --repo ClearMeasureLabs/bootcamp-palermo-workorders
gh variable set UAT_RESOURCE_GROUP_NAME --env UAT --body bootcamp-uat --repo ClearMeasureLabs/bootcamp-palermo-workorders
gh variable set CONTAINER_APP_NAME --env Prod --body ui-gh --repo ClearMeasureLabs/bootcamp-palermo-workorders
gh variable set PROD_RESOURCE_GROUP_NAME --env Prod --body bootcamp-prod --repo ClearMeasureLabs/bootcamp-palermo-workorders
```

## Verification

After the next Deploy run (or a re-run of UAT/Prod jobs), each job log must contain `Container App URL: https://…` and must not show the skip warnings above. Optional: `GET {url}/_healthcheck` returns HTTP 200.
