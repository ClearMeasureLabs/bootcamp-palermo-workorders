# Function Point Analysis — Church Bulletin / Work Order System

Sizing of this solution using **IFPUG Function Point Analysis** (Counting Practices
Manual 4.3.1), the method popularized and extended by **Capers Jones**. The count is
**apportioned per deployed process** so it can be overlaid on the runtime (C4 container)
architecture diagram. See `arch-c4-container-deployment.puml` / `.md`.

> Date counted: 2026-06-30. Boundary: the Church Bulletin work-order application and its
> deployable units. Count type: **Application (baseline) count**, unadjusted.

---

## 1. Background: Capers Jones and IFPUG Function Points

**Function Point Analysis (FPA)** measures the *functional size* of software from the
**user's point of view** — what the software does, independent of language, platform, or
lines of code. It was invented by **Allan Albrecht at IBM (1979)** and is maintained today
as an ISO/IEC 20926 standard by the **International Function Point Users Group (IFPUG)**.

**Capers Jones** is the software-measurement researcher most responsible for turning
function points into an industry benchmarking instrument. His contributions relevant here:

- **Backfiring** — empirical conversion ratios between function points and source lines of
  code per language (for C#, roughly **~50–55 LOC per FP**), letting legacy code be sized.
- **Industry benchmarks** — defect density (defects/FP), productivity (FP/staff-month), and
  cost (\$/FP) tables derived from thousands of projects.
- He documents IFPUG counting productivity at roughly **400–600 FP per day** for an
  experienced counter, and treats FP as the denominator for nearly all software economics.

### IFPUG function types

A count is built from **five function types**, grouped into data and transactions:

| Group | Type | Meaning |
|-------|------|---------|
| **Data** | **ILF** — Internal Logical File | A user-recognizable group of logically related data **maintained inside** the application boundary. |
| **Data** | **EIF** — External Interface File | Data **referenced but maintained by another** application. |
| **Transaction** | **EI** — External Input | An elementary process that **maintains** an ILF or alters system behavior (create/update/delete). |
| **Transaction** | **EO** — External Output | An elementary process that sends data outside the boundary **with derived data, calculation, or processing** beyond simple retrieval. |
| **Transaction** | **EQ** — External Inquiry | An elementary process that **retrieves** data and sends it outside the boundary with **no derivation** (read-only). |

### IFPUG complexity weights (unadjusted function points)

Each function is rated **Low / Average / High** by its DETs (Data Element Types — unique
user-recognizable fields), and for data functions its RETs (Record Element Types), or for
transactions its FTRs (File Types Referenced). The standard weight table:

| Type | Low | Average | High |
|------|-----|---------|------|
| ILF | 7 | 10 | 15 |
| EIF | 5 | 7 | 10 |
| EI  | 3 | 4 | 6 |
| EO  | 4 | 5 | 7 |
| EQ  | 3 | 4 | 6 |

Summing the weights of every identified function gives the **Unadjusted Function Point
(UFP)** count. (An optional Value Adjustment Factor from 14 General System Characteristics
can scale it ±35%; modern IFPUG and Capers Jones generally report **unadjusted** FP, which
is what is used below.)

---

## 2. Data Functions (ILF / EIF)

User-recognizable persisted data groups. All are maintained inside the boundary, so all are
**ILFs**; the system references no externally-maintained data files, so **EIF = 0**.
Each data group has 1 RET and well under 20 DETs → **Low complexity (7 FP)**.

| # | Data Function | Type | Key DETs (fields) | RET | Complexity | FP | Stored in |
|---|---------------|------|-------------------|-----|------------|----|-----------|
| 1 | **WorkOrder** | ILF | Number, Title, Description, RoomNumber, Status, Creator, Assignee, CreatedDate, AssignedDate, CompletedDate (~10) | 1 | Low | 7 | Database |
| 2 | **WorkOrderAttachment** | ILF | FileName, ContentType, FileSize, UploadedById, UploadedDate, WorkOrderId (~6) | 1 | Low | 7 | Database |
| 3 | **Employee** | ILF | UserName, FirstName, LastName, EmailAddress, PreferredLanguage, Roles (~6) | 1 | Low | 7 | Database |
| 4 | **Role** | ILF | Name, CanCreateWorkOrder, CanFulfillWorkOrder (3) | 1 | Low | 7 | Database |
| 5 | **AiBotWorkOrderSagaState** | ILF | SagaId, WorkOrderNumber, WorkOrder (~3) | 1 | Low | 7 | NSB Transport (owned by Worker) |

**Data total: 5 ILF, 0 EIF = 35 FP.** (28 FP physically in the SQL database; 7 FP in the
saga store owned by the Worker process.)

---

## 3. Transactional Functions (EI / EO / EQ), by deployed process

Each elementary process is attributed to the process that **executes** it. Where the same
logical operation is exposed through two deployables (e.g. "create work order" via both the
UI/API and the MCP agent interface), each deployed surface is counted, because each is
separately delivered functionality — see the note in §5.

### 3.1 UI Server (ASP.NET / Azure Container App — Blazor Server + REST API + AI gateway)

| Function | Type | Cplx | FP |
|----------|------|------|----|
| Save draft / create work order (`SaveDraftCommand`) | EI | Avg | 4 |
| Assign work order (`DraftToAssignedCommand`) | EI | Avg | 4 |
| Begin work (`AssignedToInProgressCommand`) | EI | Low | 3 |
| Complete work (`InProgressToCompleteCommand`) | EI | Low | 3 |
| Cancel (`AssignedToCancelledCommand`) | EI | Low | 3 |
| Shelve / return to assigned (`InProgressToAssignedCommand`) | EI | Low | 3 |
| Add attachment metadata (`AddAttachmentMetadataCommand`) | EI | Avg | 4 |
| Bulk CSV import of work orders (file in, per-row create, error report) | EI | High | 6 |
| Update settings / preferred language | EI | Low | 3 |
| Login / authenticate user | EI | Low | 3 |
| Work order search (filter creator/assignee/status → list) | EQ | Avg | 4 |
| View / manage single work order by number | EQ | Avg | 4 |
| My work orders (current user) | EQ | Low | 3 |
| Last work order component | EQ | Low | 3 |
| List employees (dropdown options) | EQ | Low | 3 |
| List work order attachments | EQ | Avg | 4 |
| Ping | EQ | Low | 3 |
| Version | EQ | Low | 3 |
| Time | EQ | Low | 3 |
| Diagnostics / feature flags | EQ | Low | 3 |
| Simple health | EQ | Low | 3 |
| Weather forecast (computed sample data) | EO | Low | 4 |
| Application AI chat (LLM + tools, derived output) | EO | High | 7 |
| Work order AI chat (LLM + work-order tools) | EO | Avg | 5 |
| Translation / speak text (LLM translation service) | EO | Avg | 5 |
| Detailed health report (aggregated/derived) | EO | Avg | 5 |

**UI Server subtotal: 10 EI (36) + 11 EQ (36) + 5 EO (26) = 98 FP**

### 3.2 MCP Server (Model Context Protocol tools + resources for AI agents)

| Function | Type | Cplx | FP |
|----------|------|------|----|
| `create-work-order` | EI | Avg | 4 |
| `execute-work-order-command` (6 commands, validation, branching) | EI | High | 6 |
| `list-work-orders` (optional status filter) | EQ | Avg | 4 |
| `get-work-order` | EQ | Avg | 4 |
| `list-work-order-attachments` | EQ | Avg | 4 |
| `list-employees` | EQ | Low | 3 |
| `get-employee` | EQ | Low | 3 |
| Resource: work-order-statuses | EQ | Low | 3 |
| Resource: roles | EQ | Low | 3 |
| Resource: status-transitions | EQ | Low | 3 |

**MCP Server subtotal: 2 EI (10) + 8 EQ (27) = 37 FP**

### 3.3 Worker (.NET Worker Service — NServiceBus AI-bot saga)

| Function | Type | Cplx | FP |
|----------|------|------|----|
| AI-bot generate/append work-order description (LLM call) | EO | High | 7 |
| AI-bot begin work (saga → `AssignedToInProgressCommand`) | EI | Avg | 4 |
| AI-bot complete work (saga → `InProgressToCompleteCommand`) | EI | Low | 3 |
| Domain event handler (UserLoggedIn / state-transition events) | EI | Low | 3 |
| Tracer-bullet diagnostic message/reply | EQ | Low | 3 |

**Worker subtotal: 3 EI (10) + 1 EQ (3) + 1 EO (7) = 20 FP transactional**
*(+ 7 FP saga-state ILF from §2 → 27 FP attributed to Worker on the diagram.)*

### 3.4 Blazor WASM Client (browser application — client-only pages)

User-facing work-order transactions are executed server-side and counted under the UI
Server; only the client-only diagnostic pages are attributed here.

| Function | Type | Cplx | FP |
|----------|------|------|----|
| Client health check page | EQ | Low | 3 |
| Detailed client health check page | EQ | Low | 3 |

**WASM Client subtotal: 2 EQ = 6 FP**

### 3.5 Database & NServiceBus Transport

- **Database (Azure SQL):** holds 4 ILFs → **28 FP** (data functions, §2).
- **NServiceBus Transport (SQL Server):** technical message-queue / saga persistence. The
  saga-state ILF (7 FP) is attributed to the Worker that owns it; the transport itself
  delivers **0 functional FP** (pure infrastructure).

---

## 4. Totals (overlaid on the runtime diagram)

| Deployed process | EI | EQ | EO | ILF | **FP** |
|------------------|----|----|----|-----|-------:|
| UI Server | 36 | 36 | 26 | – | **98** |
| MCP Server | 10 | 27 | – | – | **37** |
| Worker | 10 | 3 | 7 | 7 | **27** |
| Database | – | – | – | 28 | **28** |
| Blazor WASM Client | – | 6 | – | – | **6** |
| NServiceBus Transport | – | – | – | – | **0** |
| **TOTAL** | **56** | **72** | **33** | **35** | **196** |

**By function type:** Data = 35 FP (5 ILF, 0 EIF) · Transactional = 161 FP (15 EI, 22 EQ, 6 EO).

### **Total Unadjusted Function Points (UFP) = 196**

**Capers Jones backfiring sanity check:** at ~50–55 C# LOC/FP, 196 UFP ≈ **9,800–10,800 LOC**
of equivalent functional code — consistent with a system of this scope.

---

## 5. Notes, assumptions, and counting decisions

1. **Per-process apportioning vs. single-application count.** Strict IFPUG counts one
   application boundary and would record each *logical* elementary process once. This report
   intentionally apportions by deployable so the runtime diagram can show "FP per process."
   Functionality intentionally duplicated across channels — the **MCP agent interface
   re-exposes create / command / list operations already offered by the UI/API** — is
   counted on each surface. Collapsed to a single boundary (deduplicating the ~37 FP of MCP
   re-exposed functions against their UI equivalents), the application count is **≈ 150–160
   UFP**. The 196 figure reflects total delivered function across all deployables.
2. **Complexity ratings** use field/FTR counts from the source; all data groups are small
   (1 RET, <20 DET) so all rate **Low**. Transaction ratings lean conservative.
3. **Diagnostic / health / ping / version endpoints** are included as Low EQ/EO. Some FPA
   practitioners exclude pure infrastructure probes; excluding the five UI Server diagnostic
   EQs (Ping, Version, Time, Diagnostics, Simple Health = 15 FP) and the Worker tracer-bullet
   (3 FP) and the two WASM health pages (6 FP) would reduce the total by 24 FP to **172 UFP**.
4. **Unadjusted** count reported (no VAF applied), per common Capers Jones practice.
5. `WeatherForecast` and the `Counter`/`FetchData` sample pages are scaffolding; the weather
   sample is counted (it is a live endpoint), the template counter/fetchdata demo pages are
   not counted as business function.

---

## 6. Sources

- IFPUG, *Function Point Counting Practices Manual (CPM) 4.3.1* — function types and the
  ILF/EIF/EI/EO/EQ weight tables.
- Capers Jones, *Applied Software Measurement* and *Software Assessments, Benchmarks, and
  Best Practices* — backfiring (LOC↔FP), benchmarks, and counting-rate guidance.
- ISO/IEC 20926 — IFPUG functional size measurement method.
