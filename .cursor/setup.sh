#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# Cursor Cloud Agent – environment setup script
#
# Installs and configures:
#   1. .NET 10 SDK (10.0.1xx channel, matching global.json)
#   2. PowerShell 7 (required for PrivateBuild.ps1)
#   3. Docker Engine with fuse-overlayfs + iptables-legacy
#   4. NuGet restore
# ---------------------------------------------------------------------------

DOTNET_INSTALL_DIR="$HOME/.dotnet"
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$DOTNET_INSTALL_DIR/tools:$PATH"

# ── 1. .NET 10 SDK ─────────────────────────────────────────────────────────
if ! command -v dotnet &>/dev/null || ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
    echo "==> Installing .NET 10 SDK..."
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_INSTALL_DIR"
    rm -f /tmp/dotnet-install.sh
fi
echo "    dotnet $(dotnet --version)"

# Persist PATH for subsequent shell sessions
grep -q 'DOTNET_ROOT' "$HOME/.bashrc" 2>/dev/null || \
    echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\"" >> "$HOME/.bashrc"
grep -q '\.dotnet/tools' "$HOME/.bashrc" 2>/dev/null || \
    echo "export PATH=\"$DOTNET_INSTALL_DIR:\$HOME/.dotnet/tools:\$PATH\"" >> "$HOME/.bashrc"

# ── 2. PowerShell 7 ────────────────────────────────────────────────────────
if ! command -v pwsh &>/dev/null; then
    echo "==> Installing PowerShell 7..."
    . /etc/os-release 2>/dev/null || true
    if [ "${ID:-}" = "ubuntu" ]; then
        sudo apt-get update -qq
        sudo apt-get install -y -qq wget apt-transport-https software-properties-common >/dev/null 2>&1 || true
        DEB_URL="https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb"
        wget -q "$DEB_URL" -O /tmp/packages-microsoft-prod.deb
        sudo dpkg -i /tmp/packages-microsoft-prod.deb
        rm -f /tmp/packages-microsoft-prod.deb
        sudo apt-get update -qq
        sudo apt-get install -y -qq powershell >/dev/null
    else
        dotnet tool install --global PowerShell
    fi
fi
echo "    $(pwsh --version)"

# ── 3. Docker Engine ───────────────────────────────────────────────────────
if ! command -v dockerd &>/dev/null; then
    echo "==> Installing Docker..."
    curl -fsSL https://get.docker.com | sudo sh
fi

if ! command -v fuse-overlayfs &>/dev/null; then
    echo "==> Installing fuse-overlayfs..."
    sudo apt-get update -qq
    sudo apt-get install -y -qq fuse-overlayfs >/dev/null
fi

if [ ! -f /etc/docker/daemon.json ] || ! grep -q fuse-overlayfs /etc/docker/daemon.json 2>/dev/null; then
    sudo mkdir -p /etc/docker
    echo '{"storage-driver": "fuse-overlayfs"}' | sudo tee /etc/docker/daemon.json >/dev/null
    sudo pkill dockerd 2>/dev/null || true
    sleep 2
fi

if command -v iptables-legacy &>/dev/null; then
    sudo update-alternatives --set iptables /usr/sbin/iptables-legacy 2>/dev/null || true
    sudo update-alternatives --set ip6tables /usr/sbin/ip6tables-legacy 2>/dev/null || true
fi

if ! docker info &>/dev/null; then
    echo "==> Starting Docker daemon..."
    sudo dockerd &>/dev/null &
    for i in 1 2 3 4 5 6; do
        sleep 2
        if docker info &>/dev/null; then break; fi
    done
    sudo chmod 666 /var/run/docker.sock
fi
echo "    Docker $(docker --version)"

# ── 4. NuGet restore ──────────────────────────────────────────────────────
echo "==> Restoring NuGet packages..."
dotnet restore src/ChurchBulletin.sln

echo ""
echo "==> Environment setup complete."
