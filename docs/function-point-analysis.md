# Function Point Analysis — Work Order Management Application

**Date:** 2026-02-27
**Methodology:** IFPUG 4.3.1 (with Capers Jones backfiring cross-validation)
**Application Type:** Online business application, medium complexity
**Application Boundary:** All .NET projects in src/ including UI, API, Worker, LlmGateway, McpServer, DataAccess, Core

---

## Executive Summary

| Measure | Value |
|---------|-------|
| **Unadjusted Function Points (UFP)** | **94** |
| **Value Adjustment Factor (VAF)** | **1.11** |
| **Adjusted Function Points (AFP)** | **104** |
| Backfired Estimate (from C# LOC) | 96 FP |

### Function Point Distribution by Category

| Category | Count | Function Points | % of Total |
|----------|-------|----------------|------------|
| Internal Logical Files (ILF) | 3 | 21 | 22.3% |
| External Interface Files (EIF) | 1 | 5 | 5.3% |
| External Inputs (EI) | 10 | 38 | 40.4% |
| External Outputs (EO) | 2 | 11 | 11.7% |
| External Inquiries (EQ) | 6 | 19 | 20.2% |
| **Total (Unadjusted)** | **22** | **94** | **100%** |

---

## 1. Internal Logical Files (ILFs) — 21 FP

ILFs are user-identifiable groups of logically related data maintained within the application boundary.

| # | ILF | Data Element Types (DETs) | Record Element Types (RETs) | Complexity | FP |
|---|-----|--------------------------|----------------------------|------------|-----|
| 1 | **WorkOrder** | 11 — Id, Number, Title, Description, RoomNumber, Status, CreatorId, AssigneeId, AssignedDate, CreatedDate, CompletedDate | 1 | Low | 7 |
| 2 | **Employee** | 5 — Id, UserName, FirstName, LastName, EmailAddress | 2 — Employee record, EmployeeRoles subgroup | Low | 7 |
| 3 | **Role** | 4 — Id, Name, CanCreateWorkOrder, CanFulfillWorkOrder | 1 | Low | 7 |
| | **ILF Subtotal** | | | | **21** |

### Database Tables Supporting ILFs

| Table | Schema | Source |
|-------|--------|--------|
| `dbo.WorkOrder` | Id, Number, Title, Description, Status (char 3), RoomNumber, CreatorId (FK), AssigneeId (FK), CreatedDate, AssignedDate, CompletedDate | Migration 003-019 |
| `dbo.Employee` | Id, UserName, FirstName, LastName, EmailAddress | Migration 003 |
| `dbo.Role` | Id, Name, CanCreateWorkOrder, CanFulfillWorkOrder | Migration 010 |
| `dbo.EmployeeRoles` | EmployeeId (FK), RoleId (FK) — Junction table, counted as RET of Employee | Migration 011 |

### Not Counted as ILFs

- **WeatherForecast** — Generated in-memory, not persisted to database
- **NServiceBus tables** (`nServiceBus.*`) — Infrastructure/middleware, not user-identifiable business data
- **WorkOrderStatus** — Code-level value object with static instances, not a database-maintained entity

---

## 2. External Interface Files (EIFs) — 5 FP

EIFs are user-identifiable groups of logically related data referenced by the application but maintained externally.

| # | EIF | Data Element Types (DETs) | Record Element Types (RETs) | Complexity | FP |
|---|-----|--------------------------|----------------------------|------------|-----|
| 1 | **Azure OpenAI LLM Service** | 5 — Prompt, ChatHistory, SystemContext, ResponseText, ModelId | 2 — ChatMessage, ChatHistoryMessage | Low | 5 |
| | **EIF Subtotal** | | | | **5** |

### Notes on EIF Determination

- Azure OpenAI is the sole external system whose data store (the trained model/knowledge base) is referenced but not maintained by this application
- The SQL Server database is maintained BY the application, so it is reflected in the ILFs, not as an EIF
- NServiceBus transport (SQL Server-based) is internal infrastructure within the application boundary
- Azure Monitor/OpenTelemetry is an operational monitoring service, not a business data source

---

## 3. External Inputs (EIs) — 38 FP

EIs are elementary processes where the primary intent is to maintain an ILF or alter system behavior. Data or control information enters from outside the application boundary.

| # | EI | DETs | FTRs | Complexity | FP | Source |
|---|-----|------|------|------------|-----|--------|
| 1 | **Login** (select employee, authenticate) | 2 | 1 (Employee) | Low | 3 | Login.razor |
| 2 | **Logout** (clear authentication state) | 1 | 0 | Low | 3 | Logout.razor |
| 3 | **Create New Work Order** (Save Draft — New mode) | 6 | 2 (WorkOrder, Employee) | Average | 4 | WorkOrderManage.razor (mode=New) |
| 4 | **Save Existing Draft** (Save Draft — Edit mode) | 6 | 2 (WorkOrder, Employee) | Average | 4 | WorkOrderManage.razor (mode=Edit) |
| 5 | **Assign Work Order** (Draft → Assigned) | 6 | 2 (WorkOrder, Employee) | Average | 4 | DraftToAssignedCommand |
| 6 | **Begin Work Order** (Assigned → InProgress) | 6 | 2 (WorkOrder, Employee) | Average | 4 | AssignedToInProgressCommand |
| 7 | **Complete Work Order** (InProgress → Complete) | 6 | 2 (WorkOrder, Employee) | Average | 4 | InProgressToCompleteCommand |
| 8 | **Cancel Work Order** (Assigned/InProgress → Cancelled) | 6 | 2 (WorkOrder, Employee) | Average | 4 | AssignedToCancelled / InProgressToCancelled |
| 9 | **Shelve Work Order** (InProgress → Assigned) | 6 | 2 (WorkOrder, Employee) | Average | 4 | InProgressToAssigned |
| 10 | **AI Bot Auto-Process Work Order** (saga: Assign→Begin→AI→Complete) | 5 | 3 (WorkOrder, Employee, Azure OpenAI) | Average | 4 | AiBotWorkOrderSaga |
| | **EI Subtotal** | | | | **38** |

### EI Complexity Determination

For work order state transitions (#3-9), each submits the full form (Title, Description, RoomNumber, AssignedToUserName, WorkOrderNumber, action verb = 6 DETs) and references 2 FTRs (WorkOrder ILF, Employee ILF). Per the IFPUG EI complexity matrix: 5-15 DETs with 2 FTRs = **Average** (4 FP each).

Each state command represents a distinct elementary process with unique:
- Validation logic (different begin status, different user role requirements)
- Processing logic (different end status, different date fields set)
- Side effects (e.g., DraftToAssigned may trigger WorkOrderAssignedToBotEvent)

### MCP Server Tools

The MCP Server exposes 5 work order tools and 2 employee tools. These tools invoke the same underlying commands and queries as the Blazor UI. Per IFPUG rules, the same elementary process accessible through multiple interfaces is counted **once**. The MCP tools do not add additional EIs but do increase architectural complexity (reflected in the VAF).

---

## 4. External Outputs (EOs) — 11 FP

EOs are elementary processes that send data or control information outside the application boundary. The primary intent is to present information through processing logic beyond simple retrieval (i.e., derived data).

| # | EO | DETs | FTRs | Complexity | FP | Source |
|---|-----|------|------|------------|-----|--------|
| 1 | **Work Order AI Chat Response** (contextual AI assistance per work order) | 5 | 3 (WorkOrder, Employee, Azure OpenAI) | Low | 4 | WorkOrderChat.razor → WorkOrderChatHandler |
| 2 | **Application AI Chat Response** (general AI assistant with tool use) | 7 | 4 (WorkOrder, Employee, Role, Azure OpenAI) | High | 7 | ApplicationChat.razor → ApplicationChatHandler |
| | **EO Subtotal** | | | | **11** |

### EO vs EQ Distinction

The AI chat responses are classified as EOs (not EQs) because they produce **derived data**: the AI model processes the user prompt against work order context and generates a synthesized natural-language response. This involves complex external processing (Azure OpenAI) beyond simple data retrieval.

The Application Chat (#2) is rated **High** complexity because it has access to 7 AI tools (via ToolProvider/IToolProvider) that can query and modify all three ILFs, resulting in 4+ FTRs and 6+ DETs.

---

## 5. External Inquiries (EQs) — 19 FP

EQs are elementary processes that retrieve and present data without deriving it or altering an ILF. Complexity is the **higher** of the input-side and output-side ratings.

| # | EQ | Input DETs/FTRs | Output DETs/FTRs | Input Complexity | Output Complexity | Final | FP | Source |
|---|-----|----------------|-----------------|-----------------|------------------|-------|-----|--------|
| 1 | **View Work Order Detail** | 1 / 0 | 11 / 2 | Low | Average | **Average** | 4 | WorkOrderManage (Edit mode) |
| 2 | **Search Work Orders** (with status/creator/assignee filters) | 3 / 2 | 5 / 2 | Low | Low | **Low** | 3 | WorkOrderSearch.razor |
| 3 | **List All Employees** (login dropdown, assignee dropdown) | 0 / 0 | 5 / 2 | Low | Low | **Low** | 3 | EmployeeGetAllQuery |
| 4 | **Get Employee by Username** | 1 / 0 | 5 / 2 | Low | Low | **Low** | 3 | EmployeeByUserNameQuery |
| 5 | **Weather Forecast Display** | 0 / 0 | 5 / 0 | Low | Low | **Low** | 3 | FetchData.razor |
| 6 | **My Work Orders Count** (nav component) | 1 / 1 | 1 / 1 | Low | Low | **Low** | 3 | MyWorkOrders.razor |
| | **EQ Subtotal** | | | | | | **19** |

### Notes

- **View Work Order Detail** (#1) returns 11 DETs across 2 FTRs (WorkOrder + Employee data via Creator/Assignee), making the output side Average complexity
- **Weather Forecast** (#5) does not reference an ILF (data is generated in-memory); included per Capers Jones practical counting guidelines though it would be excluded under strict IFPUG rules
- Pre-filtered nav shortcuts (My Work Orders, Assigned to Me, All Assigned, In Progress) all use the same Search Work Orders EQ (#2) with preset parameters — counted once per IFPUG uniqueness rules

---

## 6. Value Adjustment Factor (VAF) — 1.11

The 14 General System Characteristics are rated 0-5 based on their degree of influence.

| # | General System Characteristic | Rating | Justification |
|---|------------------------------|--------|---------------|
| 1 | Data Communications | 4 | HTTP/HTTPS, Blazor SignalR WebSocket, NServiceBus SQL transport, MCP protocol, REST API |
| 2 | Distributed Data Processing | 4 | Worker service processes asynchronously, NServiceBus message routing, MCP Server, RemotableBus HTTP bridge |
| 3 | Performance | 2 | Async processing throughout, background saga orchestration |
| 4 | Heavily Used Configuration | 2 | Azure Container Apps, multi-environment deployment (TDD/UAT/Prod) |
| 5 | Transaction Rate | 2 | Medium-volume work order management system |
| 6 | Online Data Entry | 5 | Interactive Blazor forms with DataAnnotations validation, real-time chat, state-aware action buttons |
| 7 | End-User Efficiency | 4 | Pre-filtered nav shortcuts, AI chat assistant, role-based UI, auto-populated dropdowns |
| 8 | Online Update | 3 | Real-time Blazor state management, work order state machine, event-driven component updates |
| 9 | Complex Processing | 3 | State machine with 8 transitions, NServiceBus saga orchestration, AI tool-calling integration |
| 10 | Reusability | 3 | Onion Architecture, shared UI component library, MCP tool reuse, IBus abstraction |
| 11 | Installation Ease | 3 | Docker containerized, DbUp automated migrations, Azure Container Apps deployment |
| 12 | Operational Ease | 3 | 5 health checks, diagnostics endpoint, OpenTelemetry tracing, Application Insights |
| 13 | Multiple Sites | 4 | Azure cloud deployment across TDD, UAT, and Production environments |
| 14 | Facilitate Change | 4 | Strict Onion Architecture, CQRS/MediatR, Lamar DI, clean dependency flow |
| | **Total Degree of Influence (TDI)** | **46** | |

**VAF = (TDI × 0.01) + 0.65 = (46 × 0.01) + 0.65 = 1.11**

**Adjusted Function Points = 94 × 1.11 ≈ 104 AFP**

> Note: IFPUG 4.3.1 deprecated the VAF. Modern practice often reports only UFP. Both values are provided for completeness.

---

## 7. Backfiring Cross-Validation (Capers Jones Method)

Capers Jones's backfiring technique estimates function points from lines of code using language-specific ratios.

### Lines of Code (Production Only, Excluding Tests)

| Language | LOC | Capers Jones Ratio (LOC/FP) | Estimated FP |
|----------|-----|----------------------------|-------------|
| C# | 5,267 | 55 | 95.8 |
| Razor/Blazor | 1,204 | 50 (mixed markup/code) | 24.1 |
| SQL (migrations) | 1,427 | 13 | 109.8 |
| **Total** | **7,898** | | |

### Backfired Estimate

Using C# as the primary implementation language:

- **C# only**: 5,267 ÷ 55 = **~96 FP**
- **C# + Razor blended** (58 LOC/FP): 6,471 ÷ 58 = **~112 FP**

The backfired range of **96–112 FP** aligns well with the detailed IFPUG count of **94 UFP / 104 AFP**, providing confidence in the analysis.

> SQL lines are excluded from the primary backfiring because migration scripts overlap with the C#/EF Core implementations of the same ILFs. Counting both would double-count.

---

## 8. Work Order State Machine — Function Point Impact

The work order lifecycle is a key driver of the function point count, contributing 7 of 10 EIs:

```
                  ┌──────────────┐
                  │    Draft     │
                  └──────┬───────┘
                         │ Assign (EI #5)
                  ┌──────▼───────┐
             ┌────│   Assigned   │────┐
             │    └──────┬───────┘    │
    Cancel   │           │ Begin      │ Cancel
    (EI #8)  │    ┌──────▼───────┐    │ (EI #8)
             │    │  InProgress  │────┘
             │    └──┬───────┬───┘
             │       │       │ Shelve (EI #9)
             │       │       └──────► Assigned
             ▼  Complete (EI #7)
    ┌────────────┐   │
    │ Cancelled  │   ▼
    └────────────┘ ┌──────────────┐
                   │  Complete    │
                   └──────────────┘
```

Additionally:
- **Save Draft** (EI #3/#4): Creates or updates work orders in Draft status
- **AI Bot Auto-Process** (EI #10): Automated saga traversing Assigned → InProgress → Complete

---

## 9. Application Boundary Diagram

```
┌─────────────────────────────────────────────────────┐
│              APPLICATION BOUNDARY                     │
│                                                       │
│  ┌─────────┐  ┌──────────┐  ┌─────────────────┐     │
│  │  Core   │  │DataAccess│  │   LlmGateway    │     │
│  │ (Domain)│◄─│ (EF Core)│  │(Azure OpenAI    │     │
│  └─────────┘  └──────────┘  │ client)         │     │
│       ▲            ▲         └────────┬────────┘     │
│       │            │                  │              │
│  ┌────┴────────────┴───┐    ┌────────▼────────┐     │
│  │   UI (Blazor WASM   │    │    Worker       │     │
│  │   + Server + API)   │    │  (NServiceBus)  │     │
│  └─────────────────────┘    └─────────────────┘     │
│       ▲         ▲                                    │
│       │         │           ┌─────────────────┐     │
│       │         │           │   McpServer     │     │
│       │         │           │  (MCP Protocol) │     │
│       │         │           └─────────────────┘     │
└───────┼─────────┼───────────────────────────────────┘
        │         │
   ┌────▼───┐ ┌───▼────────────┐
   │ Users  │ │ Azure OpenAI   │
   │(Browser│ │ (EIF)          │
   └────────┘ └────────────────┘
```

### ILFs (Inside Boundary)
- WorkOrder (7 FP), Employee (7 FP), Role (7 FP)

### EIFs (Outside Boundary, Referenced)
- Azure OpenAI LLM Service (5 FP)

---

## 10. Category Summary and Interpretation

### By Function Type

| Function Type | Count | FP | Avg FP/Function |
|---------------|-------|-----|-----------------|
| Data Functions (ILF + EIF) | 4 | 26 | 6.5 |
| Transactional Functions (EI + EO + EQ) | 18 | 68 | 3.8 |
| **Total** | **22** | **94** | **4.3** |

### Capers Jones Size Classification

| Size Category | FP Range | This Application |
|---------------|----------|-----------------|
| Small | 1–100 | ← **94 UFP** |
| Medium | 100–1,000 | ← **104 AFP** |
| Large | 1,000–10,000 | |
| Very Large | 10,000–100,000 | |

This application sits at the small-to-medium boundary. The 94 UFP reflects a focused domain (work order management) with moderate complexity from the state machine workflow, AI integration, distributed messaging (NServiceBus saga), and multi-channel access (Blazor UI, MCP Server, API).

### Productivity Benchmarks (Capers Jones)

For an online business application of this size and medium complexity, Capers Jones industry benchmarks suggest:

| Metric | Industry Average | This Application |
|--------|-----------------|-----------------|
| Development effort | ~6-10 FP/staff-month | ~94 FP total |
| Defect potential | ~4-5 defects/FP | ~400-470 potential defects |
| Defect removal efficiency | 85-95% (with testing) | 3 test levels (unit, integration, acceptance) |
| Maintenance effort | ~8-10% of dev effort/year | Expected |

---

## Appendix A — Complete Function Point Inventory

### A.1 ILFs

| ID | Name | DETs | RETs | Complexity | FP |
|----|------|------|------|------------|-----|
| ILF-1 | WorkOrder | 11 | 1 | Low | 7 |
| ILF-2 | Employee | 5 | 2 | Low | 7 |
| ILF-3 | Role | 4 | 1 | Low | 7 |

### A.2 EIFs

| ID | Name | DETs | RETs | Complexity | FP |
|----|------|------|------|------------|-----|
| EIF-1 | Azure OpenAI LLM Service | 5 | 2 | Low | 5 |

### A.3 EIs

| ID | Name | DETs | FTRs | Complexity | FP |
|----|------|------|------|------------|-----|
| EI-1 | Login | 2 | 1 | Low | 3 |
| EI-2 | Logout | 1 | 0 | Low | 3 |
| EI-3 | Create New Work Order | 6 | 2 | Average | 4 |
| EI-4 | Save Existing Draft | 6 | 2 | Average | 4 |
| EI-5 | Assign Work Order | 6 | 2 | Average | 4 |
| EI-6 | Begin Work Order | 6 | 2 | Average | 4 |
| EI-7 | Complete Work Order | 6 | 2 | Average | 4 |
| EI-8 | Cancel Work Order | 6 | 2 | Average | 4 |
| EI-9 | Shelve Work Order | 6 | 2 | Average | 4 |
| EI-10 | AI Bot Auto-Process | 5 | 3 | Average | 4 |

### A.4 EOs

| ID | Name | DETs | FTRs | Complexity | FP |
|----|------|------|------|------------|-----|
| EO-1 | Work Order AI Chat | 5 | 3 | Low | 4 |
| EO-2 | Application AI Chat | 7 | 4 | High | 7 |

### A.5 EQs

| ID | Name | Input DETs/FTRs | Output DETs/FTRs | Complexity | FP |
|----|------|----------------|-----------------|------------|-----|
| EQ-1 | View Work Order Detail | 1/0 | 11/2 | Average | 4 |
| EQ-2 | Search Work Orders | 3/2 | 5/2 | Low | 3 |
| EQ-3 | List All Employees | 0/0 | 5/2 | Low | 3 |
| EQ-4 | Get Employee by Username | 1/0 | 5/2 | Low | 3 |
| EQ-5 | Weather Forecast | 0/0 | 5/0 | Low | 3 |
| EQ-6 | My Work Orders Count | 1/1 | 1/1 | Low | 3 |
