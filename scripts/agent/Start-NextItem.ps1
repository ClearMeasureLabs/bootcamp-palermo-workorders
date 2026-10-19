# v33g: multi-item sessions (session-size experiment). Run after an item's RESULT line, before the next item's Get-Context.
# Resets src/ to the starting ref (Publish-AgentPr diffs the whole workspace src, so leftovers would leak into the next PR),
# resets the Get-Context call counter, and marks the session as multi-item (Wait-PrChecks then says to continue, not stop).
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
$start = 'origin/master'
git reset -q -- src 2>$null | Out-Null
git checkout -q $start -- src
git clean -fdq -- src
$tmp = [IO.Path]::GetTempPath()
Remove-Item (Join-Path $tmp 'agent-getcontext-count*'), (Join-Path $tmp 'agent-getcontext-issue') -ErrorAction SilentlyContinue
Set-Content (Join-Path $tmp 'agent-multi-item') '1'
$left = @(git status --porcelain -- src).Count
if ($left) { "NEXT_ITEM=FAIL ($left src changes remain)" } else { "NEXT_ITEM=READY (src reset to $start; Get-Context counter reset)" }
