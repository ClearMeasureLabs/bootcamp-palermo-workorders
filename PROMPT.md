## Work Item Context

- **Issue:** #8179 — [FST] Add /api/hello endpoint returning greeting - 20260729-153409-1of1
- **URL:** https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/8179
- **Previous Status:** Test Design
- **Current Status:** Development
- **Repository:** ClearMeasureLabs/bootcamp-palermo-workorders
- **Workflow Key:** complete:8179:Test Design
- **Occurred At:** 2026-07-29T15:38:47.3310074+00:00
- **AI Factory Label:** AI Factory
- **Ready to Move Label:** Ready To Move
- **AI Factory API URL:** https://aisoftwarefactory-jeffreyalienware.ngrok.app

---

Development task: implement the work item in the repository already cloned in your workspace. Use ONLY the AI Factory callback API for everything outside git (`AI_FACTORY_API_URL` from the Work Item Context above) — work item reads/comments/labels AND all pull-request operations (`/api/tools/workitems/{id}/pullrequests` create/get/checks/merge). Do NOT call api.github.com for PR operations. Be terse; journal at phase transitions only.

From the Work Item Context read: `AI_FACTORY_API_URL`, `ISSUE_NUMBER`, `AUTO_MERGE_LABEL`. Branch name: `ibmbob/$ISSUE_NUMBER-development`.

**FIX-FORWARD MODE (only if the Work Item Context contains `EXISTING_PR_NUMBER` / `EXISTING_PR_BRANCH` / `EXISTING_PR_URL`):** this item was DEMOTED from Functional Validation because its PR could not be merged. Do the following instead of the numbered steps below, and DO the rest of the polling/complete flow on the SAME PR:
- Do NOT create a new branch or a new PR. NEVER merge the PR — merges happen ONLY in Functional Validation, performed by the factory, never by you.
- Check out `EXISTING_PR_BRANCH`. Bring it up to date by MERGING master INTO the branch: `git merge origin/master`. **Do NOT rebase and do NOT force-push** — a rebase rewrites pushed history and invalidates review context and in-flight checks.
- Resolve any merge conflicts, preserving BOTH the feature changes and master's changes.
- Address the newest `🤖` demotion comment on the work item (it states the merge-failure reason).
- Push normally (`git push`, never `--force`).
- Poll `GET .../pullrequests/$EXISTING_PR_NUMBER/checks` (as in step 5) and, ONLY on green, `POST .../workitems/$ISSUE_NUMBER/complete`. Completion means "the PR is mergeable again"; the item returns to Functional Validation where the review re-runs and the FACTORY merges. Skip step 6 entirely (you never merge in fix-forward mode).

If `EXISTING_PR_*` is ABSENT, follow the normal first-pass flow below unchanged.

1. `GET .../workitems/$ISSUE_NUMBER` — read requirements (concept + UX/technical/test design sections). Read `AGENTS.md` if present.
2. Branch off the base branch; implement the change with tests, following existing patterns. Every commit message contains `#$ISSUE_NUMBER` and `[AB#$ISSUE_NUMBER]`. Push.
3. Run the repo's private build (`pwsh ./PrivateBuild.ps1` or per AGENTS.md). Fix until green — do NOT open a PR on a red build.
4. Create the PR via callback: `POST .../workitems/$ISSUE_NUMBER/pullrequests` with `{"headBranch":"ibmbob/$ISSUE_NUMBER-development","baseBranch":null,"title":"<issue title> (#$ISSUE_NUMBER)","body":"<short summary + files + testing notes>","draft":false}`. Save `pullRequest.number` and `url`; journal one comment with the URL.
5. Poll `GET .../pullrequests/$PR_NUMBER/checks` every 30s (≤30 tries) until `checks.status` != "pending". On `failure`: diagnose, fix, commit, push, repeat. ONLY on `success`: make ONE final call — `POST .../workitems/$ISSUE_NUMBER/complete` with `{"comment": "🤖 PR open, CI green, ready for review — <PR link>"}`. This posts the journal comment AND signals the factory, which advances the item deterministically. NEVER call it before checks report success; on unrecoverable failure call it with `"succeeded": false` and a failure summary instead.
6. Auto-merge ONLY if the work item's labels include `AUTO_MERGE_LABEL`: `POST .../pullrequests/$PR_NUMBER/merge` with `{"mergeMethod":"squash"}` (retry ≤10× on 30s if checks pending), then journal the merge. Otherwise the open green PR is the final state.

If a step fails after 3 retries, post one failure comment and stop without the label.
