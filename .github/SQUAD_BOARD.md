# Squad label and project board guidance

Label created/verified: $labelName (color #0E8A16)

This file explains how to map the squad label to a project board column so issues labeled squad appear in the Squad column.

If using Projects (v2):
1. Open the repository → Projects → pick the Project (v2).
2. Create a column or Single-select field named Squad.
3. In the Project -> Automation settings, add a rule: "When an issue is added or updated and has label squad, move the item to column Squad."

To automate via GraphQL (example mutation — requires API token with project access):

# 1) Find the project v2 id and the field id for the single-select column named 'Squad'
# Query (replace owner/repo as needed):
# gh api graphql -f query='query { repository(owner: "ClearMeasureLabs", name: "bootcamp-palermo-workorders") { projectsV2(first:10) { nodes { id title fields(first:50) { nodes { ... on ProjectV2SingleSelectField { id name } } } } } } }'

# 2) Create an automation via the Projects v2 API is not currently exposed as a simple REST endpoint; use the UI to add the automation rule.

For classic Projects (if in use):

# Create a column named 'Squad' using the gh CLI (classic projects):
# gh project list --repo ClearMeasureLabs/bootcamp-palermo-workorders
# gh project view "<PROJECT_NAME>" --repo ClearMeasureLabs/bootcamp-palermo-workorders --json number --jq '.number'
# gh project column create --project NUMBER --name "Squad"

# After creating the column, set up automation in the classic project UI: "Move issues with label 'squad' to this column".

## Commands used by this script (already executed):
# - gh api POST /repos/ClearMeasureLabs/bootcamp-palermo-workorders/labels -f name='squad' -f color='0E8A16' -f description='Work assigned to the Squad team (use with squad:{member} sub-labels if desired)'

