#!/usr/bin/env bash
# Writes the Octopus build-information JSON consumed by the Codefresh step
# octopusdeploy-push-build-information (contract §7.7, handoff 4), in the format
# documented at https://octopus.com/docs/packaging-applications/build-servers/build-information:
#   BuildEnvironment, BuildNumber, BuildUrl, Branch, VcsType, VcsRoot,
#   VcsCommitNumber, Commits[{Id, Comment}]
# Commits cover HEAD^1..HEAD: for a merge commit on master, the pull request's
# commits plus the merge itself. Octopus Insights measures lead time from the
# earliest commit in build information, and links "#1234" references to issues.
#
# Optionally also writes the release notes for octopusdeploy-create-release
# (RELEASE_NOTES_FILE). Their first line is "app-commit: <sha>", which the
# Octopus step report-commit-status reads (contract §7.2 step 12). A file is
# used because the step renders RELEASE_NOTES into generated YAML, where ": "
# breaks parsing (see .codefresh/README.md, contract gaps).
#
# Usage: buildinfo.sh --out <json-file> [--release-notes-out <file>] [--repo <dir>]
# Requires: git, jq.
set -euo pipefail

die() {
  printf 'buildinfo.sh: %s\n' "$1" >&2
  exit 1
}

out=""
notes_out=""
repo_root=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --out)
      [ "$#" -ge 2 ] || die "--out needs a file"
      out="$2"
      shift 2
      ;;
    --release-notes-out)
      [ "$#" -ge 2 ] || die "--release-notes-out needs a file"
      notes_out="$2"
      shift 2
      ;;
    --repo)
      [ "$#" -ge 2 ] || die "--repo needs a value"
      repo_root="$2"
      shift 2
      ;;
    *)
      die "unknown argument: $1"
      ;;
  esac
done

[ -n "$out" ] || die "--out is required"
command -v jq >/dev/null 2>&1 || die "jq is required"

if [ -z "$repo_root" ]; then
  repo_root="$(git rev-parse --show-toplevel)" || die "not inside a git repository"
fi
cd "$repo_root" || die "cannot enter $repo_root"

head_sha="$(git rev-parse HEAD)"
branch="${CF_BRANCH:-$(git rev-parse --abbrev-ref HEAD)}"
vcs_root="https://github.com/${CF_REPO_OWNER:-ClearMeasureLabs}/${CF_REPO_NAME:-bootcamp-palermo-workorders}"

if git rev-parse --verify --quiet 'HEAD^1' >/dev/null; then
  range='HEAD^1..HEAD'
else
  range='HEAD'   # root commit: only itself
fi

shas_file="$(mktemp)"
commits_file="$(mktemp)"
trap 'rm -f "$shas_file" "$commits_file"' EXIT

# Separate commands, so that set -e stops on any git failure instead of
# writing build information with a silently empty commit list.
git log --format=%H "$range" >"$shas_file"
while IFS= read -r sha; do
  [ -n "$sha" ] || continue
  comment="$(git log -1 --format=%B "$sha")"
  jq -n --arg id "$sha" --arg comment "$comment" '{Id: $id, Comment: $comment}'
done <"$shas_file" >"$commits_file"

mkdir -p "$(dirname "$out")"
jq -n \
  --arg environment "Codefresh" \
  --arg number "${CF_BUILD_ID:-local}" \
  --arg url "${CF_BUILD_URL:-}" \
  --arg branch "$branch" \
  --arg root "$vcs_root" \
  --arg head "$head_sha" \
  --slurpfile commits "$commits_file" \
  '{
     BuildEnvironment: $environment,
     BuildNumber: $number,
     BuildUrl: $url,
     Branch: $branch,
     VcsType: "Git",
     VcsRoot: $root,
     VcsCommitNumber: $head,
     Commits: $commits
   }' >"$out"
printf 'buildinfo.sh: wrote %s (%s commit(s), range %s)\n' "$out" "$(jq '.Commits | length' "$out")" "$range" >&2

if [ -n "$notes_out" ]; then
  mkdir -p "$(dirname "$notes_out")"
  {
    printf 'app-commit: %s\n' "$head_sha"
    if [ -n "${CF_BUILD_URL:-}" ]; then
      printf 'build: %s\n' "$CF_BUILD_URL"
    fi
  } >"$notes_out"
  printf 'buildinfo.sh: wrote %s\n' "$notes_out" >&2
fi
