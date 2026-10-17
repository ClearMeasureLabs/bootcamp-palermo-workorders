# Commit, push, open the PR against master, and post one session comment on the issue. Prints the PR URL only.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [Parameter(Mandatory)][string]$Title,
      [string]$AgentUrl = "", [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
# Branch from master so the PR carries only the src edit (not the analysis-branch commits).
git fetch -q origin master
git stash push -q -- src
git checkout -q -B $Branch origin/master
git stash pop -q
git add -A src | Out-Null
git commit -q -m "$Title (#$Issue)"
git push -q -u origin $Branch 2>&1 | Out-Null
$body = "Closes #$Issue`n`nAutomated change via lean feature loop. Build + filtered unit tests passed locally; full gates run in CI."
$pr = gh pr create --repo $Repo --base master --head $Branch --title $Title --body $body
gh issue comment $Issue --repo $Repo --body "Cursor session: $(if ($AgentUrl) { $AgentUrl } else { "(posted by dispatcher)" })`nBranch: $Branch`nPR: $pr" | Out-Null
"PR=$pr"
