# platform/ci-dotnet: one toolchain image for
#   - every Codefresh gate in workorders/ci and workorders/release, and
#   - the Octopus steps migrate-database and acceptance-tests
#     (step container #{StepImage.CiDotnet}; ADR-C2, ADR-C11).
# Built and signed keyless by Codefresh workorders/ci-image, tag <ci-image-version>
# (date-based, contract §7.5). Build context: .codefresh/images/ci-dotnet/.
#
# Contents: .NET SDK 10 (ships pwsh), Playwright 1.54 browsers and their OS
# dependencies, go-sqlcmd, Azure CLI, git, jq, and the supply-chain tools the
# release pipeline runs (Gitleaks, Syft, cosign, crane). The dotnet global tools
# that build.ps1 and scripts/crap install on every run are pre-installed.
#
# Users: the image defaults to the non-root `app` user. Codefresh runs every step
# container as root regardless ("We run all containers with the root user ... to
# allow ... mounting the shared volume", Codefresh KB "Run docker containers as
# non root user"), so build.ps1's `playwright install chromium --with-deps` (apt)
# works there; the Octopus script pods run as `app` and never call apt.
# Placeholders (<...>) are pinned when the image is first built [VERIFY each digest].

# Browsers for Microsoft.Playwright 1.54.0 (app:src/AcceptanceTests uses
# microsoft.playwright.nunit 1.54.0); this image keeps them in /ms-playwright.
FROM mcr.microsoft.com/playwright/dotnet:v1.54.0-noble@sha256:<playwright-dotnet-v1.54.0-noble-digest> AS browsers
# Only Chromium is used by the acceptance suite; drop the other engines before copying.
RUN rm -rf /ms-playwright/firefox-* /ms-playwright/webkit-*

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:<dotnet-sdk-10.0-digest>

ARG VERSION=0.0.0-local
ARG REVISION=unknown
ARG GO_SQLCMD_VERSION=<go-sqlcmd-version>
ARG GO_SQLCMD_SHA256=<go-sqlcmd-linux-amd64-sha256>
ARG AZURE_CLI_VERSION=<azure-cli-version>
ARG GITLEAKS_VERSION=8.28.0
ARG GITLEAKS_SHA256=<gitleaks-8.28.0-linux-x64-sha256>
ARG SYFT_VERSION=<syft-version>
ARG SYFT_SHA256=<syft-linux-amd64-sha256>
ARG COSIGN_VERSION=<cosign-version>
ARG COSIGN_SHA256=<cosign-linux-amd64-sha256>
ARG CRANE_VERSION=<crane-version>
ARG CRANE_SHA256=<go-containerregistry-linux-x86_64-sha256>
ARG OCTOPUS_DOTNET_CLI_VERSION=<octopus-dotnet-cli-version>

LABEL org.opencontainers.image.source="https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders" \
      org.opencontainers.image.revision="${REVISION}" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.title="platform/ci-dotnet" \
      org.opencontainers.image.description="CI and Octopus step toolchain for Work Orders: .NET 10 SDK, pwsh, Playwright 1.54, go-sqlcmd, Azure CLI"

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    MSBUILDTERMINALLOGGER=off \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    DEBIAN_FRONTEND=noninteractive

SHELL ["/bin/bash", "-o", "pipefail", "-c"]

# Base tools plus the Chromium runtime libraries for Ubuntu 24.04 (noble). The list
# matches the one proven with Playwright 1.54 on this base in
# app:.bob/skills/ai-factory-executor/Dockerfile.agent.claude.
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      ca-certificates curl git jq unzip bzip2 gnupg lsb-release \
      fonts-liberation libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 \
      libdbus-1-3 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 \
      libpango-1.0-0 libcairo2 libasound2t64 libatspi2.0-0 libxshmfence1 \
 && rm -rf /var/lib/apt/lists/*

# Azure CLI from Microsoft's signed apt repository (read-deployment-secrets and ad-hoc checks).
RUN mkdir -p /etc/apt/keyrings \
 && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /etc/apt/keyrings/microsoft.gpg \
 && chmod go+r /etc/apt/keyrings/microsoft.gpg \
 && printf 'Types: deb\nURIs: https://packages.microsoft.com/repos/azure-cli/\nSuites: %s\nComponents: main\nArchitectures: %s\nSigned-by: /etc/apt/keyrings/microsoft.gpg\n' \
      "$(lsb_release -cs)" "$(dpkg --print-architecture)" > /etc/apt/sources.list.d/azure-cli.sources \
 && apt-get update \
 && apt-get install -y --no-install-recommends "azure-cli=${AZURE_CLI_VERSION}-1~$(lsb_release -cs)" \
 && rm -rf /var/lib/apt/lists/* \
 && az version --output none

# go-sqlcmd: build.ps1 uses `sqlcmd` against the external SQL Server service and then
# skips installing the ~219 MB SqlServer PowerShell module (BuildFunctions.ps1).
RUN curl -fsSL -o /tmp/sqlcmd.tar.bz2 \
      "https://github.com/microsoft/go-sqlcmd/releases/download/v${GO_SQLCMD_VERSION}/sqlcmd-linux-amd64.tar.bz2" \
 && echo "${GO_SQLCMD_SHA256}  /tmp/sqlcmd.tar.bz2" | sha256sum -c - \
 && tar -xjf /tmp/sqlcmd.tar.bz2 -C /usr/local/bin sqlcmd \
 && rm /tmp/sqlcmd.tar.bz2 \
 && sqlcmd --version

# Gitleaks (security_scan), Syft and cosign (supply_chain), crane (digest lookup).
RUN curl -fsSL -o /tmp/gitleaks.tar.gz \
      "https://github.com/gitleaks/gitleaks/releases/download/v${GITLEAKS_VERSION}/gitleaks_${GITLEAKS_VERSION}_linux_x64.tar.gz" \
 && echo "${GITLEAKS_SHA256}  /tmp/gitleaks.tar.gz" | sha256sum -c - \
 && tar -xzf /tmp/gitleaks.tar.gz -C /usr/local/bin gitleaks \
 && curl -fsSL -o /tmp/syft.tar.gz \
      "https://github.com/anchore/syft/releases/download/v${SYFT_VERSION}/syft_${SYFT_VERSION}_linux_amd64.tar.gz" \
 && echo "${SYFT_SHA256}  /tmp/syft.tar.gz" | sha256sum -c - \
 && tar -xzf /tmp/syft.tar.gz -C /usr/local/bin syft \
 && curl -fsSL -o /usr/local/bin/cosign \
      "https://github.com/sigstore/cosign/releases/download/v${COSIGN_VERSION}/cosign-linux-amd64" \
 && echo "${COSIGN_SHA256}  /usr/local/bin/cosign" | sha256sum -c - \
 && chmod 0755 /usr/local/bin/cosign \
 && curl -fsSL -o /tmp/crane.tar.gz \
      "https://github.com/google/go-containerregistry/releases/download/v${CRANE_VERSION}/go-containerregistry_Linux_x86_64.tar.gz" \
 && echo "${CRANE_SHA256}  /tmp/crane.tar.gz" | sha256sum -c - \
 && tar -xzf /tmp/crane.tar.gz -C /usr/local/bin crane \
 && rm -f /tmp/*.tar.gz \
 && gitleaks version && syft version && cosign version && crane version

# Playwright browsers (Chromium and its headless shell), readable by every user.
COPY --from=browsers /ms-playwright /ms-playwright
RUN chmod -R a+rX /ms-playwright

# dotnet global tools that CI would otherwise download on every run. Codefresh steps
# run as root, so they are installed for root (build.ps1 and run-crap-audit.ps1 add
# $HOME/.dotnet/tools to PATH themselves). Versions match scripts/crap/run-crap-audit.ps1.
RUN dotnet tool install --global Octopus.DotNet.Cli --version "${OCTOPUS_DOTNET_CLI_VERSION}" \
 && dotnet tool install --global crap4dotnet --version 0.1.1 \
 && dotnet tool install --global dotnet-script --version 2.0.0

# Non-root default user for every runtime other than Codefresh (Octopus script pods,
# local `docker run`). .NET 8+ images define `app`; create it only if it is missing.
RUN id -u app >/dev/null 2>&1 || useradd --uid 1654 --create-home --shell /bin/bash app
USER app
WORKDIR /home/app
