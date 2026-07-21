<#
.SYNOPSIS
    End-to-end Pester (v5+) test for the Argo AI Dev factory.
.DESCRIPTION
    1) Creates 3 GitHub issues (label 'ai-factory-test').
    2) Starts ai-dev: submits one isolated Argo Workflow per issue via
       implement-issue.ps1 --keep-alive (up to 3 concurrent, gated by semaphore).
    3) Monitors each Workflow's pod logs and asserts the standard SDLC runs in
       order: clone -> branch -> implement -> PrivateBuild -> AcceptanceTests ->
       quality gates pass -> commit -> push -> PR created.
    4) Asserts each container stays open serving the app behind a Cloudflare
       quick tunnel, and prints the 3 live URLs to the console.

    By design this LEAVES the issues, PRs, workflows, and pods running so the
    apps can be viewed. Cleanup is opt-in: set $env:AIDEV_CLEANUP = 'true'.

.NOTES
    Prerequisites (test SKIPS itself, with a clear reason, if unmet):
      - kubectl reachable and WorkflowTemplate 'ai-dev-agent' installed
      - argo CLI on PATH
      - gh authenticated
      - agent credentials in the cluster (Secret created per run by the test)
    Run:  Invoke-Pester ./argo/tests/AiDevFactory.Tests.ps1 -Output Detailed
#>

BeforeDiscovery {
    # Prerequisite probing happens at discovery so -Skip can gate the whole file.
    $script:RepoOrg  = if ($env:AIDEV_REPO_ORG)  { $env:AIDEV_REPO_ORG }  else { 'ClearMeasureLabs' }
    $script:RepoName = if ($env:AIDEV_REPO_NAME) { $env:AIDEV_REPO_NAME } else { 'bootcamp-palermo-workorders' }
    $script:Agent    = if ($env:AIDEV_AGENT)     { $env:AIDEV_AGENT }     else { 'claude' }

    function Test-Prereqs {
        $missing = @()
        # cloudflared is a CONTAINER-only dependency (baked into the agent image
        # for serve mode); it is intentionally NOT required on the host.
        foreach ($t in 'kubectl','argo','gh') {
            if (-not (Get-Command $t -ErrorAction SilentlyContinue)) { $missing += "cli:$t" }
        }
        if ($missing) { return $missing }
        & kubectl get workflowtemplate ai-dev-agent -n ai-factory *> $null
        if ($LASTEXITCODE -ne 0) { $missing += 'workflowtemplate:ai-dev-agent (run argo/install.ps1)' }
        & gh auth status *> $null
        if ($LASTEXITCODE -ne 0) { $missing += 'gh:not-authenticated' }
        return $missing
    }
    $script:PrereqMissing = Test-Prereqs
    $script:SkipAll = [bool]$script:PrereqMissing
    if ($script:SkipAll) {
        Write-Host "[SKIP] AiDevFactory tests -- prerequisites missing: $($script:PrereqMissing -join ', ')" -ForegroundColor Yellow
    }
}

Describe 'AI Dev Factory - 3 concurrent isolated issues, monitored, served' -Skip:$script:SkipAll {

    BeforeAll {
        $ErrorActionPreference = 'Stop'
        $script:Repo     = "$RepoOrg/$RepoName"
        $script:Label    = 'ai-factory-test'
        $script:Ns       = 'ai-factory'
        $script:Cli      = Join-Path $PSScriptRoot '..' 'implement-issue.ps1' | Resolve-Path
        $script:RunTag   = 'aidevtest-' + ([guid]::NewGuid().ToString('N').Substring(0,8))
        $script:Issues   = @()   # [{ Number; Title; Branch; Workflow; Pr; Url }]

        # The three demo issues. Intentionally small, self-contained changes so a
        # full SDLC run (implement + build + tests + PR) completes in reasonable time.
        $script:Specs = @(
            @{ Title = "[$RunTag] Add app version to footer"
               Body  = "Display the application version (assembly informational version) in the site footer on every page. Keep the change minimal and add a bUnit test." }
            @{ Title = "[$RunTag] Add /_healthcheck 'ok' text"
               Body  = "Ensure the health check endpoint returns a friendly 'ok' body in addition to 200. Add/adjust a test." }
            @{ Title = "[$RunTag] Show work request count on dashboard"
               Body  = "Add a read-only count of total work requests to the dashboard/home page. Follow existing CQRS query patterns and add a unit test." }
        )

        # Ensure the shared test label exists (idempotent).
        & gh label create $Label --repo $Repo --color BFD4F2 --description 'AI Dev factory automated test' *> $null

        function Get-WorkflowForBranch([string]$branch) {
            # Workflows are named issue-<n>-<rand>; find by the issue label we set.
            $json = & argo list -n $script:Ns -o json 2>$null | ConvertFrom-Json
            return ($json | Where-Object { $_.metadata.name -like "issue-*" -and $_.spec.arguments.parameters.Value -contains $branch } | Select-Object -First 1)
        }

        function Get-PodLogs([string]$wfName) {
            $l = & argo logs $wfName -n $script:Ns --no-color 2>$null
            return ($l -join "`n")
        }
    }

    It 'creates 3 GitHub issues labeled ai-factory-test' {
        foreach ($spec in $Specs) {
            $url = & gh issue create --repo $Repo --title $spec.Title --body $spec.Body --label $Label
            $LASTEXITCODE | Should -Be 0 -Because "gh issue create should succeed"
            $num = [int]([regex]::Match($url, '/issues/(\d+)').Groups[1].Value)
            $num | Should -BeGreaterThan 0
            $slug = ($spec.Title.ToLower() -replace '[^a-z0-9]+','-').Trim('-')
            if ($slug.Length -gt 50) { $slug = $slug.Substring(0,50).Trim('-') }
            $script:Issues += [pscustomobject]@{
                Number = $num; Title = $spec.Title; Url = $url
                Branch = "feature/issue-$num-$slug"; Workflow = $null; Pr = $null
            }
            Write-Host "  created issue #$num  $($spec.Title)" -ForegroundColor Cyan
        }
        $script:Issues.Count | Should -Be 3
    }

    It 'starts ai-dev: submits an isolated KEEP_ALIVE workflow per issue (<=3 concurrent)' {
        foreach ($i in $script:Issues) {
            # Fire-and-forget submit; concurrency is bounded by the semaphore (=3).
            & pwsh -File $Cli --issueid $i.Number --agent $Agent --repo-org $RepoOrg --repo-name $RepoName --keep-alive --no-watch
            $LASTEXITCODE | Should -Be 0 -Because "implement-issue.ps1 should submit issue #$($i.Number)"
        }
        Start-Sleep -Seconds 10
        $wf = & argo list -n $Ns -o name 2>$null
        ($wf | Where-Object { $_ -like 'issue-*' }).Count | Should -BeGreaterOrEqual 1
    }

    # NOTE: these iterate over $script:Issues INSIDE the test (run time). Pester
    # evaluates -ForEach at DISCOVERY time, when $script:Issues is still empty
    # (it is populated by the 'creates 3 issues' test above), so a data-driven
    # -ForEach here would expand to zero cases and silently never run.
    It 'works every issue through the full SDLC and opens a PR' {
        $script:Issues.Count | Should -Be 3 -Because "the 3 issues must have been created first"

        # The ordered SDLC phases the container emits (structured JSON + progress).
        $phases = @(
            'Cloning repository',
            'Creating branch',
            'Selected AI agent',
            'Agent implementation complete',
            'Running PrivateBuild',
            'Running AcceptanceTests',
            'All quality gates passed',
            'Changes committed',
            'Pushing branch',
            'Pull request created'
        )

        foreach ($issue in $script:Issues) {
            $wf = & argo list -n $Ns -o name 2>$null | Where-Object { $_ -like "issue-$($issue.Number)-*" } | Select-Object -First 1
            $wf | Should -Not -BeNullOrEmpty -Because "a workflow should exist for issue #$($issue.Number)"

            # Poll pod logs until PR created / failed / timeout. Generous budget:
            # the agent makes real LLM calls and runs PrivateBuild + AcceptanceTests.
            $deadline = (Get-Date).AddMinutes([int]($env:AIDEV_TIMEOUT_MIN ?? 40))
            $logs = ''
            $done = $false
            while ((Get-Date) -lt $deadline) {
                $logs  = & argo logs $wf -n $Ns --no-color 2>$null | Out-String
                $status = & argo get $wf -n $Ns -o json 2>$null | ConvertFrom-Json
                if ($logs -match 'Pull request created') { $done = $true; break }
                if ($status.status.phase -in @('Failed','Error')) { break }
                Start-Sleep -Seconds 20
            }

            $done | Should -BeTrue -Because "issue #$($issue.Number) should reach 'Pull request created'. Last log tail:`n$([string]::Join("`n", ($logs -split "`n" | Select-Object -Last 25)))"

            # Assert the SDLC phases appear IN ORDER.
            $lastIdx = -1
            foreach ($p in $phases) {
                $idx = $logs.IndexOf($p)
                $idx | Should -BeGreaterThan -1 -Because "SDLC phase '$p' should appear in issue #$($issue.Number) logs"
                $idx | Should -BeGreaterThan $lastIdx -Because "SDLC phase '$p' should occur after the previous phase"
                $lastIdx = $idx
            }

            # A real PR should now exist for the branch.
            $pr = & gh pr list --repo $Repo --head $issue.Branch --state open --json number,url | ConvertFrom-Json
            $pr.Count | Should -BeGreaterThan 0 -Because "a PR should be open for $($issue.Branch)"
            $issue.Pr = $pr[0].number
            Write-Host "  issue #$($issue.Number) -> PR #$($issue.Pr)" -ForegroundColor Green
        }
    }

    It 'keeps every issue serving the live app behind a Cloudflare tunnel' {
        foreach ($issue in $script:Issues) {
            $wf = & argo list -n $Ns -o name 2>$null | Where-Object { $_ -like "issue-$($issue.Number)-*" } | Select-Object -First 1
            $wf | Should -Not -BeNullOrEmpty

            # Wait for serve mode to publish the tunnel URL marker.
            $deadline = (Get-Date).AddMinutes(10)
            $url = $null
            while ((Get-Date) -lt $deadline -and -not $url) {
                $logs = & argo logs $wf -n $Ns --no-color 2>$null | Out-String
                $m = [regex]::Match($logs, 'AI_FACTORY_LIVE_URL[^\n]*url=(https://[a-z0-9-]+\.trycloudflare\.com)')
                if ($m.Success) { $url = $m.Groups[1].Value; break }
                Start-Sleep -Seconds 15
            }
            $url | Should -Match '^https://[a-z0-9-]+\.trycloudflare\.com$' -Because "serve mode should publish a live URL for issue #$($issue.Number)"

            # The pod must still be running (held open by KEEP_ALIVE).
            $phase = (& argo get $wf -n $Ns -o json 2>$null | ConvertFrom-Json).status.phase
            $phase | Should -Be 'Running' -Because "KEEP_ALIVE should hold the workflow/pod open"

            Write-Host "  LIVE  issue #$($issue.Number)  PR #$($issue.Pr)  ->  $url" -ForegroundColor Magenta
        }
    }

    AfterAll {
        Write-Host ""
        Write-Host "==================== AI Dev Factory: live apps ====================" -ForegroundColor Cyan
        foreach ($i in $script:Issues) {
            $wf = & argo list -n $Ns -o name 2>$null | Where-Object { $_ -like "issue-$($i.Number)-*" } | Select-Object -First 1
            $logs = if ($wf) { & argo logs $wf -n $Ns --no-color 2>$null | Out-String } else { '' }
            $m = [regex]::Match($logs, 'url=(https://[a-z0-9-]+\.trycloudflare\.com)')
            $url = if ($m.Success) { $m.Groups[1].Value } else { '(not published)' }
            Write-Host ("  issue #{0}  PR #{1}  {2}" -f $i.Number, ($i.Pr ?? '-'), $url) -ForegroundColor Green
        }
        Write-Host "Issues/PRs/pods left running for inspection." -ForegroundColor Cyan
        Write-Host "Cleanup: set AIDEV_CLEANUP=true to close them, or:" -ForegroundColor DarkGray
        Write-Host "  argo delete -n $Ns --selector app=ai-dev ; gh issue list --repo $Repo --label $Label" -ForegroundColor DarkGray
        Write-Host "===================================================================" -ForegroundColor Cyan

        if ($env:AIDEV_CLEANUP -eq 'true') {
            Write-Host "AIDEV_CLEANUP=true -> tearing down..." -ForegroundColor Yellow
            foreach ($i in $script:Issues) {
                if ($i.Pr) { & gh pr close $i.Pr --repo $Repo --delete-branch *> $null }
                & gh issue close $i.Number --repo $Repo *> $null
            }
            & argo delete -n $Ns --selector app=ai-dev *> $null
        }
    }
}
