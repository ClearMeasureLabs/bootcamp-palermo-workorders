<#
Install and start k3s inside the Ubuntu WSL2 distro (pilot P2 — see deploy/PILOT.md).

Idempotent: safe to re-run. Already-completed steps are skipped.
State at authoring time (2026-07-29): k3s v1.36.2+k3s1 binary is downloaded,
sha256-verified against the official release checksum, and installed to
/usr/local/bin with kubectl/crictl/ctr symlinks; the systemd unit is authored
at /tmp/k3s.service. This script finishes the job: installs the unit, starts
the service, and waits for the node to be Ready.

Run from the repo root:  .\deploy\install-k3s-wsl.ps1
Teardown:                wsl -d Ubuntu -- sudo systemctl disable --now k3s
#>
$ErrorActionPreference = 'Stop'
$distro = 'Ubuntu'
$version = 'v1.36.2+k3s1'
$versionUrl = $version -replace '\+', '%2B'

function Invoke-Wsl([string]$cmd) {
    & wsl.exe -d $distro -- sh -c $cmd
    if ($LASTEXITCODE -ne 0) { throw "WSL command failed ($LASTEXITCODE): $cmd" }
}

Write-Host "== 1/5 k3s binary =="
& wsl.exe -d $distro -- sh -c 'command -v k3s >/dev/null 2>&1'
if ($LASTEXITCODE -ne 0) {
    Write-Host "Downloading k3s $version and verifying sha256..."
    Invoke-Wsl "cd /tmp && curl -fsSLo k3s 'https://github.com/k3s-io/k3s/releases/download/$versionUrl/k3s' && curl -fsSLo k3s.sha256 'https://github.com/k3s-io/k3s/releases/download/$versionUrl/sha256sum-amd64.txt' && grep -E ' k3s\$' k3s.sha256 | sha256sum -c -"
    Invoke-Wsl "sudo install -m 755 /tmp/k3s /usr/local/bin/k3s && sudo ln -sf /usr/local/bin/k3s /usr/local/bin/kubectl && sudo ln -sf /usr/local/bin/k3s /usr/local/bin/crictl && sudo ln -sf /usr/local/bin/k3s /usr/local/bin/ctr"
} else {
    Write-Host "k3s binary already installed: $(& wsl.exe -d $distro -- k3s --version | Select-Object -First 1)"
}

Write-Host "== 2/5 systemd unit =="
& wsl.exe -d $distro -- sh -c 'test -f /etc/systemd/system/k3s.service'
if ($LASTEXITCODE -ne 0) {
    # Unit content matches what the official get.k3s.io installer generates.
    & wsl.exe -d $distro -- sh -c 'test -f /tmp/k3s.service'
    if ($LASTEXITCODE -ne 0) {
        Invoke-Wsl @'
cat > /tmp/k3s.service <<'"'"'EOF'"'"'
[Unit]
Description=Lightweight Kubernetes
Documentation=https://k3s.io
Wants=network-online.target
After=network-online.target

[Install]
WantedBy=multi-user.target

[Service]
Type=notify
EnvironmentFile=-/etc/systemd/system/k3s.service.env
KillMode=process
Delegate=yes
LimitNOFILE=1048576
LimitNPROC=infinity
LimitCORE=infinity
TasksMax=infinity
TimeoutStartSec=0
Restart=always
RestartSec=5s
ExecStartPre=-/sbin/modprobe br_netfilter
ExecStartPre=-/sbin/modprobe overlay
ExecStart=/usr/local/bin/k3s server --write-kubeconfig-mode 644
EOF
'@
    }
    Invoke-Wsl 'sudo cp /tmp/k3s.service /etc/systemd/system/k3s.service && sudo touch /etc/systemd/system/k3s.service.env && sudo systemctl daemon-reload'
} else {
    Write-Host "Unit already present."
}

Write-Host "== 3/5 enable + start k3s =="
Invoke-Wsl 'sudo systemctl enable --now k3s'

Write-Host "== 4/5 wait for node Ready (up to 120s) =="
$deadline = (Get-Date).AddSeconds(120)
$ready = $false
while ((Get-Date) -lt $deadline) {
    $nodes = & wsl.exe -d $distro -- sh -c 'k3s kubectl get nodes --no-headers 2>/dev/null'
    if ($LASTEXITCODE -eq 0 -and $nodes -match '\sReady\s') { $ready = $true; break }
    Start-Sleep -Seconds 5
}
if (-not $ready) {
    & wsl.exe -d $distro -- sh -c 'systemctl status k3s --no-pager | tail -15; journalctl -u k3s --no-pager | tail -20'
    throw "Node did not reach Ready within 120s — status above."
}
& wsl.exe -d $distro -- sh -c 'k3s kubectl get nodes'

Write-Host "== 5/5 system pods =="
& wsl.exe -d $distro -- sh -c 'k3s kubectl get pods -A'

Write-Host ""
Write-Host "k3s is up. Kubeconfig: /etc/rancher/k3s/k3s.yaml (inside $distro)." -ForegroundColor Green
Write-Host "Next: deploy/PILOT.md P3 (image build/push) and P4 (kubectl apply -k deploy/overlays/dev)." -ForegroundColor Green
