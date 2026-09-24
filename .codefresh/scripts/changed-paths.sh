#!/usr/bin/env bash
# Prints the paths changed by the commit under test, one per line, for
#   .github/scripts/detect-code-changes.sh --from-list -
# (the existing docs-only classifier reads the list on stdin).
#
#   master:   git diff HEAD^1 HEAD  (the first-parent diff; for a merge commit,
#             everything the pull request brings in)
#   branches: git diff $(git merge-base origin/master HEAD) HEAD  (what the
#             pull request would merge), as in build.yml job "Detect code changes"
# Codefresh exposes no equivalent of GitHub's event.before, so master uses the
# first parent instead.
#
# Fail open: on any error the script prints a sentinel path that is not
# documentation, so the classifier reports code=true and every gate runs. It
# always exits 0. An empty diff prints nothing, which the classifier treats as
# docs-only, matching build.yml.
#
# Usage: changed-paths.sh [--branch <name>] [--repo <dir>]
set -uo pipefail

# Not *.md, not docs/*, not LICENSE*, not .github/ISSUE_TEMPLATE/*: counts as code.
readonly SENTINEL=".codefresh/changed-paths-unavailable"

fail_open() {
  printf 'changed-paths.sh: %s; failing open (all gates run)\n' "$1" >&2
  printf '%s\n' "$SENTINEL"
  exit 0
}

branch="${CF_BRANCH:-}"
repo_root=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --branch)
      [ "$#" -ge 2 ] || fail_open "--branch needs a value"
      branch="$2"
      shift 2
      ;;
    --repo)
      [ "$#" -ge 2 ] || fail_open "--repo needs a value"
      repo_root="$2"
      shift 2
      ;;
    *)
      fail_open "unknown argument: $1"
      ;;
  esac
done

if [ -z "$repo_root" ]; then
  repo_root="$(git rev-parse --show-toplevel 2>/dev/null)" || fail_open "not inside a git repository"
fi
cd "$repo_root" 2>/dev/null || fail_open "cannot enter $repo_root"

if [ -z "$branch" ]; then
  branch="$(git rev-parse --abbrev-ref HEAD 2>/dev/null)" || fail_open "cannot resolve the branch"
fi

if [ "$branch" = "master" ]; then
  git rev-parse --verify --quiet 'HEAD^1' >/dev/null || fail_open "HEAD has no parent"
  base="HEAD^1"
else
  # Refresh origin/master; a failed fetch still leaves an existing ref usable.
  git fetch --no-tags --quiet origin 'master:refs/remotes/origin/master' 2>/dev/null || true
  git rev-parse --verify --quiet origin/master >/dev/null || fail_open "origin/master is unavailable"
  base="$(git merge-base origin/master HEAD 2>/dev/null)" || fail_open "git merge-base failed"
  [ -n "$base" ] || fail_open "empty merge-base"
fi

list_file="$(mktemp)" || fail_open "mktemp failed"
trap 'rm -f "$list_file"' EXIT

# --no-renames: a code-to-docs rename still lists the removed code path (as in build.yml).
git diff --name-only --no-renames "$base" HEAD >"$list_file" 2>/dev/null || fail_open "git diff failed"

cat "$list_file"
