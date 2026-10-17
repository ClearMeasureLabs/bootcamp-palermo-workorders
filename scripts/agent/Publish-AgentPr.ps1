# v5: REST only (no GraphQL), fails loudly, resumable, and refuses to open a PR whose diff touches anything outside src/.
# Iter 13 bug: the push or GraphQL `gh pr create` failed silently (push output piped to Out-Null). The agent then
# "repaired" by hand and merged analysis/token-reduction into the PR branch (54 files). Now each step checks its result,
# and a rerun with an existing branch or PR resumes instead of forcing the agent to improvise.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [Parameter(Mandatory)][string]$Title,
      [string]$AgentUrl = "", [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
function Fail($m) { "PUBLISH_FAIL: $m (do not repair by hand; reply with this line)"; exit 2 }
$owner = $Repo.Split('/')[0]
$existing = gh api "repos/$Repo/pulls?head=${owner}:$Branch&state=open" --jq '.[0].html_url // empty'
if ($existing) { "PR=$existing"; exit 0 }
git fetch -q origin master
if ((git rev-parse --abbrev-ref HEAD) -ne $Branch -or (git status --porcelain -- src)) {
  # Carry ONLY src edits as a patch; force checkout so dirty non-src files can't make checkout fail and leave HEAD on the old branch.
  git add -A src; $patch = Join-Path ([IO.Path]::GetTempPath()) 'agent-src.patch'; git diff --cached --binary --output=$patch -- src
  git checkout -q -f -B $Branch origin/master; if ($LASTEXITCODE) { Fail 'checkout' }
  if ((Get-Item $patch).Length) { git apply --index --whitespace=nowarn $patch; if ($LASTEXITCODE) { Fail 'patch apply' } }
  git add -A src | Out-Null
  if (-not (git config user.email)) { git config user.email "agent@clear-measure.com"; git config user.name "agent" }
  git commit -q -m "$Title (#$Issue)"
}
$bad = git diff --name-only origin/master...HEAD | Where-Object { $_ -notlike 'src/*' }
if ($bad) { Fail "diff has non-src files: $($bad -join ',')" }
if (-not (git diff --name-only origin/master...HEAD)) { Fail 'no src changes' }
$p = git push -q -u origin "HEAD:refs/heads/$Branch" 2>&1; if ($LASTEXITCODE) { Fail "push: $p" }
$body = "Closes #$Issue`n`nAutomated change via lean feature loop. Build + filtered unit tests passed locally; full gates run in CI."
$pr = gh api "repos/$Repo/pulls" -f title=$Title -f head=$Branch -f base=master -f body=$body --jq .html_url 2>&1
if ($LASTEXITCODE) { Fail "pr create: $pr" }
gh api "repos/$Repo/issues/$Issue/comments" -f body="Cursor session: $(if ($AgentUrl) { $AgentUrl } else { '(posted by dispatcher)' })`nBranch: $Branch`nPR: $pr" --silent 2>&1 | Out-Null
"PR=$pr"
