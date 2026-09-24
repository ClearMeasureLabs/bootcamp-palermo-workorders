#!/usr/bin/env bash
# Stages lean Docker build contexts for the Work Orders images (contract §7.5).
#
#   <out>/ui/          Dockerfile + built/
#       built/ is extracted from build/ChurchBulletin.UI.<VERSION>.nupkg exactly as
#       build.yml "Publish Release Candidate" does (F6): unzip, locate
#       ClearMeasure.Bootcamp.UI.Server.dll, copy its directory, drop the package
#       metadata. The Dockerfile is the app's root Dockerfile, copied verbatim, with
#       OCI labels appended; the tracked app:Dockerfile itself never changes.
#       --ui-source publish skips the nupkg and runs dotnet publish instead (previews).
#   <out>/worker/      Dockerfile (app:containers/worker/Dockerfile) + publish/
#       dotnet publish src/Worker (Release, --no-build) -> Worker.dll
#   <out>/db-migrator/ Dockerfile (app:containers/db-migrator/Dockerfile) + publish/ + scripts/
#       dotnet publish src/Database (Release, --no-build) plus src/Database/scripts
#
# Lean contexts keep src/**/bin and obj, video/ and the Qodana baseline out of the
# Docker build context (the app repo has no .dockerignore).
#
# Usage: stage-built.sh --version <VERSION> [--out <dir>] [--repo <dir>]
#                       [--ui-source nupkg|publish] [--only ui,worker,db-migrator]
# Requires a Release build (`. ./build.ps1; Build`) and, for --ui-source nupkg,
# `Package-Everything` with the same BUILD_BUILDNUMBER.
set -euo pipefail

die() {
  printf 'stage-built.sh: %s\n' "$1" >&2
  exit 1
}

readonly OCI_SOURCE="https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders"

version=""
out=""
repo_root=""
ui_source="nupkg"
only="ui,worker,db-migrator"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --version)
      [ "$#" -ge 2 ] || die "--version needs a value"
      version="$2"
      shift 2
      ;;
    --out)
      [ "$#" -ge 2 ] || die "--out needs a directory"
      out="$2"
      shift 2
      ;;
    --repo)
      [ "$#" -ge 2 ] || die "--repo needs a directory"
      repo_root="$2"
      shift 2
      ;;
    --ui-source)
      [ "$#" -ge 2 ] || die "--ui-source needs nupkg or publish"
      ui_source="$2"
      shift 2
      ;;
    --only)
      [ "$#" -ge 2 ] || die "--only needs a list"
      only="$2"
      shift 2
      ;;
    *)
      die "unknown argument: $1"
      ;;
  esac
done

[ -n "$version" ] || die "--version is required"
case "$ui_source" in
  nupkg | publish) ;;
  *) die "--ui-source must be nupkg or publish" ;;
esac

if [ -z "$repo_root" ]; then
  repo_root="$(git rev-parse --show-toplevel)" || die "not inside a git repository"
fi
repo_root="$(cd "$repo_root" && pwd)"

if [ -z "$out" ]; then
  if [ -n "${CF_VOLUME_PATH:-}" ]; then
    out="${CF_VOLUME_PATH}/image-contexts"
  else
    out="${repo_root}/build/image-contexts"
  fi
fi
mkdir -p "$out"
out="$(cd "$out" && pwd)"

wanted() {
  case ",${only}," in
    *",$1,"*) return 0 ;;
    *) return 1 ;;
  esac
}

# Recreates one context directory; the guard refuses an empty path.
fresh_dir() {
  local dir="${out:?}/${1:?}"
  rm -rf -- "$dir"
  mkdir -p "$dir"
  printf '%s' "$dir"
}

stage_ui() {
  local ctx
  ctx="$(fresh_dir ui)"
  mkdir -p "$ctx/built"

  if [ "$ui_source" = "nupkg" ]; then
    local pkg="$repo_root/build/ChurchBulletin.UI.${version}.nupkg"
    [ -f "$pkg" ] || die "missing $pkg; run Package-Everything with BUILD_BUILDNUMBER=${version}"

    local extract dll
    extract="$(mktemp -d)"
    unzip -q -o "$pkg" -d "$extract"
    # Octopus packages built on Windows can carry restrictive modes (build.yml does the same).
    find "$extract" -type d -exec chmod u+rwx {} +
    find "$extract" -type f -exec chmod u+r {} +

    dll="$(find "$extract" -name 'ClearMeasure.Bootcamp.UI.Server.dll' -print -quit)"
    [ -n "$dll" ] || die "ClearMeasure.Bootcamp.UI.Server.dll not found in $pkg"
    (cd "$(dirname "$dll")" && tar cf - .) | (cd "$ctx/built" && tar xf -)
    rm -rf -- "$extract"

    # Package metadata never belongs in the image.
    find "$ctx/built" -name '*.nuspec' -delete
    find "$ctx/built" -name '[[]Content_Types[]].xml' -delete
    find "$ctx/built" -name '*.psmdcp' -delete
    find "$ctx/built" -depth -type d -name '_rels' -exec rm -rf {} +
    find "$ctx/built" -depth -type d -path '*/package/services/metadata' -exec rm -rf {} +
  else
    dotnet publish "$repo_root/src/UI/Server/UI.Server.csproj" \
      --configuration Release --no-restore --no-build --nologo -o "$ctx/built"
  fi

  [ -f "$ctx/built/ClearMeasure.Bootcamp.UI.Server.dll" ] || die "ui: built/ is missing ClearMeasure.Bootcamp.UI.Server.dll"

  cp "$repo_root/Dockerfile" "$ctx/Dockerfile"
  cat >>"$ctx/Dockerfile" <<EOF

# --- Appended by .codefresh/scripts/stage-built.sh (OCI labels, contract §7.5). ---
# The tracked app Dockerfile is unchanged; only this staged copy carries the labels.
ARG VERSION=0.0.0-unset
ARG REVISION=unknown
LABEL org.opencontainers.image.source="${OCI_SOURCE}" \\
      org.opencontainers.image.revision="\${REVISION}" \\
      org.opencontainers.image.version="\${VERSION}" \\
      org.opencontainers.image.title="workorders/ui-server"
EOF
  printf 'stage-built.sh: ui context ready at %s (source: %s)\n' "$ctx" "$ui_source" >&2
}

stage_worker() {
  local ctx
  ctx="$(fresh_dir worker)"
  dotnet publish "$repo_root/src/Worker/Worker.csproj" \
    --configuration Release --no-restore --no-build --nologo -o "$ctx/publish"
  [ -f "$ctx/publish/Worker.dll" ] || die "worker: publish/ is missing Worker.dll"
  cp "$repo_root/containers/worker/Dockerfile" "$ctx/Dockerfile"
  printf 'stage-built.sh: worker context ready at %s\n' "$ctx" >&2
}

stage_migrator() {
  local ctx
  ctx="$(fresh_dir db-migrator)"
  dotnet publish "$repo_root/src/Database/Database.csproj" \
    --configuration Release --no-restore --no-build --nologo -o "$ctx/publish"
  [ -f "$ctx/publish/ClearMeasure.Bootcamp.Database.dll" ] || die "db-migrator: publish/ is missing ClearMeasure.Bootcamp.Database.dll"
  # The console reads scripts from a directory argument; the image ships them in /app/scripts.
  cp -R "$repo_root/src/Database/scripts" "$ctx/scripts"
  cp "$repo_root/containers/db-migrator/Dockerfile" "$ctx/Dockerfile"
  printf 'stage-built.sh: db-migrator context ready at %s\n' "$ctx" >&2
}

if wanted ui; then stage_ui; fi
if wanted worker; then stage_worker; fi
if wanted db-migrator; then stage_migrator; fi
