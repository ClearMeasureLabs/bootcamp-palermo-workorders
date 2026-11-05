# v6: publish WITHOUT switching the workspace branch (phase 3 rep-1 bugs: t16 published from an analysis-based branch;
# several agents got lost after the old checkout switch because scripts/agent vanished from the workspace).
# Builds the PR commit in a temp worktree on origin/master from a patch of src/ changes only. Re-running after more edits
# pushes a follow-up commit (non-force) whose tree = master + all current src changes. REST only; guarded; prints PR=<url>.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [Parameter(Mandatory)][string]$Title,
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
function Fail($m) { "PUBLISH_FAIL: $m (do not repair by hand; reply with this line)"; exit 2 }
$tmp = [IO.Path]::GetTempPath(); $patch = Join-Path $tmp 'agent-src.patch'; $wt = Join-Path $tmp 'agent-pub-wt'
git fetch -q origin master
# v43g: diff against the item branch's start commit = the local HEAD (agents never commit; the branch was cut from the skill tip).
# v42g used origin/analysis/token-reduction, else origin/master; agent VMs lacked that ref, so every round 29 PR carried skill-branch
# src (the seeding hook) and c61/c62's empty changes still published (false GREEN).
git fetch -q origin $Branch 2>$null; $start = if ($LASTEXITCODE -eq 0) { git merge-base HEAD FETCH_HEAD 2>$null } else { $null }
if (-not $start) { $start = 'HEAD' }  # merge-base = the tip the branch was cut from, even if the agent committed locally
$seed = 'scripts/agent/templates/acc-seed.patch'  # never publish the local acceptance seeding hook
if ((Test-Path $seed) -and (Select-String -Quiet 'AGENT_ACC_EXTRA_WORKORDERS' src/AcceptanceTests/ServerFixture.cs -ErrorAction SilentlyContinue)) { git apply -R $seed 2>$null | Out-Null }
# v36g: also publish .github, docs and root build scripts (round 22 c04: a workflow-only change hit NO_CHANGES)
$paths = @('src', '.github', 'docs', ':(glob)*.ps1', ':(exclude)docs/token-analysis*')
git add -A -- @paths 2>$null | Out-Null
git diff --cached --binary --output=$patch $start -- @paths
if (-not (Get-Item $patch).Length) { Fail 'no src changes' }
if (Test-Path $wt) { git worktree remove --force $wt 2>$null | Out-Null; Remove-Item -Recurse -Force $wt -ErrorAction SilentlyContinue }
git worktree prune
git worktree add -q --detach $wt origin/master 2>&1 | Out-Null; if ($LASTEXITCODE) { Fail 'worktree' }
Push-Location $wt
try {
  if (-not (git config user.email)) { git config user.email "agent@clear-measure.com"; git config user.name "agent" }
  git apply --index --3way --whitespace=nowarn $patch 2>&1 | Out-Null; if ($LASTEXITCODE) { Fail 'patch does not apply on master' }
  git commit -q -m "$Title (#$Issue)"; $tree = git rev-parse 'HEAD^{tree}'; $c = git rev-parse HEAD
  git fetch -q origin $Branch 2>$null
  if ($LASTEXITCODE -eq 0) {
    if ((git rev-parse 'FETCH_HEAD^{tree}') -eq $tree) { $c = $null } else { $c = git commit-tree $tree -p FETCH_HEAD -m "Follow-up (#$Issue)" }
  }
  if ($c) {
    $bad = git diff --name-only origin/master $c | Where-Object { $_ -notmatch '^(src|\.github|docs)/|^[^/]+\.ps1$' }
    if ($bad) { Fail "non-src files: $($bad -join ',')" }
    # v7.1: GitHub intermittently answers "! [remote rejected] ... (failed)" with no reason (phase 2 iter 14, phase 3 rep 3 t08;
    # same rules/path succeeded for the other 9 concurrent pushes). Retry with backoff before declaring PUBLISH_FAIL.
    $ok = $false
    foreach ($try in 1..4) {
      $p = git push -q origin "${c}:refs/heads/$Branch" 2>&1; if (-not $LASTEXITCODE) { $ok = $true; break }
      git fetch -q origin $Branch 2>$null
      if ($LASTEXITCODE -eq 0 -and (git rev-parse 'FETCH_HEAD^{tree}') -eq $tree) { $ok = $true; break }  # landed despite the error
      if ($try -lt 4) { Start-Sleep -Seconds (10 * $try) }
    }
    if (-not $ok) { Fail "push (4 tries): $p" }
  }
} finally { Pop-Location; git worktree remove --force $wt 2>$null | Out-Null }
$owner = $Repo.Split('/')[0]
# v26g: the open-PR lookup can come back empty transiently (round 12 s03: NO_CHANGE + empty PR= -> Submit crashed). Retry.
$pr = $null
foreach ($try in 1..3) { $pr = gh api "repos/$Repo/pulls?head=${owner}:$Branch&state=open" --jq '.[0].html_url // empty' 2>$null; if ($pr -match 'pull/\d+') { break }; $pr = $null; Start-Sleep 5 }
if (-not $pr) {
  $body = "Closes #$Issue`n`nAutomated change via lean loop. Build + unit tests passed locally; full gates run in CI."
  $pr = gh api "repos/$Repo/pulls" -f title=$Title -f head=$Branch -f base=master -f body=$body --jq .html_url 2>&1
  if ($LASTEXITCODE -or $pr -notmatch 'pull/\d+') { Fail "pr create: $pr" }
}
if ($c) { "HEAD=$c" }
if (-not $c) { "PUBLISH=NO_CHANGE (same head; to retry CI use WAIT_CMD -ForceRerun)" }
"PR=$pr"
