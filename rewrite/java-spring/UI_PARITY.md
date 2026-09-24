# Screen and workflow parity status

The source application contains more than the maintenance workflow. This matrix records what the Java web UI currently renders at the matching route and the visible or functional gaps that remain. “Partial” means there is a usable Java screen but at least one source interaction is missing.

| Source screen / route | Java route | Status | Remaining visible or functional gaps |
|---|---|---|---|
| `Index` `/` | `/` | Partial | Church welcome, nav shell, and work-order status counts are present. Source illustration, richer responsive layout, footer details, and dashboard status labels/count rules need screenshot review. |
| `Counter` `/counter` | `/counter` | Partial | Increment and reset work within the current server session. Source counter is page-local; exact persistence behavior differs. |
| `FetchData` `/fetchdata` | — | Missing | No weather forecast source or planning screen is ported. |
| `ApplicationChat` `/ai-agent` | — | Missing | AI chat and tool interaction are not ported. |
| `Settings` `/settings` | — | Missing | Employee and application settings are not ported. |
| `Login` `/login` | `/login` | Partial | Employee selection, session persistence, logout, and a demo shortcut work. Password or external identity-provider authentication is not implemented. |
| `WorkOrderSearch` `/workorder/search` | `/workorder/search` | Partial | Creator, assignee, assigned-to-me, status, overdue, and clear-filter controls plus the work-order result columns are implemented. Click-to-sort headers remain open; FetchData, AI Agent, and Settings are documented but not ported. |
| `WorkOrderManage` `/workorder/manage/{id?}` | `/workorder/manage/{number}` | Partial | Read-only fields, lifecycle commands, and attachment metadata are shown. Source edit/read-only permission behavior, due-date editing, speech/dictation, work-order chat, and binary attachment upload are not ported. |
| New Work Order (Manage create mode) | `/workorder/manage?mode=New` | Partial | Title, description, instructions, room, and due date can be created. Source voice input and exact field/action affordances are absent. |

## Parity verification in the Java acceptance run

The Playwright acceptance test captures Home, Search, Manage, and Counter screenshots at 1920×1080 and exercises login, create, creator/assignee search, assigned-to-me filtering, assignment, begin, completion, metadata attachment, and counter increment/reset. The workflow artifact must be reviewed after each UI change; this matrix is not a substitute for that visual comparison.

The Java database differences are tracked separately in [SCHEMA_PARITY.md](SCHEMA_PARITY.md). The prototype remains a partial port while any row above has open gaps.
