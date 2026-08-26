# Customer training: Grok on TDD MCP (#9105)

Independent Node/npm Remotion package. Does **not** touch `ChurchBulletin.sln`, NuGet, Core, DataAccess, McpServer, or Blazor.

**Deliverable:** customer-training mp4 — Grok (already on TDD MCP) creates Saturday lawn mows for Willie, then the same work appears on the church portal.

## Surfaces

| Side | URL |
|------|-----|
| Portal | https://ui-gh.icywave-25720613.southcentralus.azurecontainerapps.io/ |
| MCP | https://ui-gh.icywave-25720613.southcentralus.azurecontainerapps.io/mcp |

Login: `tlovejoy` / `gwillie`. No connector or prompt-setup scene.

## Composition

- Id: `TddMcpCustomerTraining`
- **1280×800 @ 30fps**, full pages letterboxed (`object-fit: contain`)
- Voice: `en-US-EmmaMultilingualNeural` via `msedge-tts`

## Layout

- `capture/` — Playwright scripts; call **live** TDD `tools/list` first; film only tools that exist; honest “not on TDD yet” caption otherwise. Never mocks MCP responses.
- `public/audio/` — narration (committed)
- `public/stills/` — freeze frames from capture (committed when capture succeeds)
- `public/footage/` — `.webm` (gitignored; regenerate with `npm run capture`)
- `artifacts/` — final customer-training mp4 + `capture-session.json` when capture runs
- `out/` — Remotion render output (gitignored)

## Commands

```bash
cd video/9105-tdd-mcp-video
npm install
npx playwright install chromium
npm run typecheck
npm run narration
# Live TDD capture (requires network egress to the Azure Container Apps host):
npm run capture
npm run render
# Copy deliverable:
cp out/customer-training-grok-tdd-mcp.mp4 artifacts/customer-training-grok-tdd-mcp.mp4
```

## Scene order

1. Intro (Grok already on TDD)
2. MCP `create-dated-work-orders` — Lovejoy → Willie, Saturday lawn mow, several dates
3. Portal search + manage proof (due-date colors)
4. MCP ops: list / get / create+instructions / save / assign·begin·complete / employees / attachments list-only
5. Portal proof for those numbers
6. Outro

Attachments: **list-only**. There is no file picker (#9104 Done as no-op). Capture does not fake an upload.

## Cloud VM note

If live Playwright capture against TDD cannot run in the cloud agent (egress / browser), the package still lands with capture scripts, narration, composition, and README. Re-run `npm run capture` from a machine that can reach the TDD host, then `npm run render`. Do **not** stub fake MCP responses into the tape.
