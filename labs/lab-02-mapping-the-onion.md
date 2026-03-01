# Lab 02: Mapping the Onion - Architecture Exploration

**Curriculum Section:** Sections 01-02 (Model of Software Leadership / Structure of a Software Project)
**Estimated Time:** 45 minutes
**Type:** Analyze / Observe

---

## Objective

Trace the Onion Architecture layers and dependency rules in the live codebase. Understand how architecture creates clarity (Pillar 1) and establishes quality (Pillar 2).

---

## Context

The Onion Architecture enforces a strict rule: **dependencies flow inward only**. The innermost layer (Core) has zero external dependencies. Outer layers reference inner layers, never the reverse. This lab maps that structure to real files.

---

## Steps

### Step 1: Study the C4 Component Diagram

Open `arch/arch-c4-component-project-dependencies.md`. Sketch the dependency arrows on paper or a whiteboard.

**Questions to answer while sketching:**
- Which project has ZERO outbound arrows?
- Which projects are in the outermost ring?

### Step 2: Verify Core Has No Project References

Open `src/Core/Core.csproj`. Look at the `<ProjectReference>` section.

**Expected finding:** There are no `<ProjectReference>` elements. Core depends on nothing.

Verify the only NuGet packages are abstractions:
- `MediatR.Contracts` (interfaces only)
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`

### Step 3: Verify DataAccess References Only Core

Open `src/DataAccess/DataAccess.csproj`. Find the `<ProjectReference>` entries.

**Expected finding:** The only project reference is `../Core/Core.csproj`.

### Step 4: Examine the Outer Layer

Open `src/UI/Server/UI.Server.csproj`. List all project references.

**Expected finding:** References to Core, DataAccess, UI.Client, UI.Api, LlmGateway, McpServer, Worker, and more. This is the composition root — the outermost layer that wires everything together.

### Step 5: Attempt an Architecture Violation

Try adding this to any file in `src/Core/`:

```csharp
using ClearMeasure.Bootcamp.DataAccess;
```

Build the project:

```powershell
dotnet build src/Core/Core.csproj
```

**Expected result:** Build failure. Core cannot reference DataAccess because no such `<ProjectReference>` exists.

**Revert the change immediately.**

### Step 6: Trace a Full Request Through the Architecture

Open `arch/WorflowForDraftToAssignedCommand.md`. Follow the Mermaid sequence diagram from User through to database.

Map each participant to its source file:

| Diagram Participant | Source File |
|---------------------|-------------|
| `SingleApiController (UI.Server)` | `src/UI/Api/Controllers/SingleApiController.cs` |
| `Bus : IBus (UI.Server)` | Interface: `src/Core/IBus.cs` |
| `DraftToAssignedCommand` | `src/Core/Model/StateCommands/DraftToAssignedCommand.cs` |
| `StateCommandHandler` | `src/DataAccess/Handlers/StateCommandHandler.cs` |
| `StateCommandBase` | `src/Core/Model/StateCommands/StateCommandBase.cs` |
| `StateCommandContext` | `src/Core/Services/StateCommandContext.cs` |
| `WorkOrder` | `src/Core/Model/WorkOrder.cs` |
| `StateCommandResult` | `src/Core/Model/StateCommands/StateCommandResult.cs` |
| `DataContext / SQL Server` | `src/DataAccess/Mappings/DataContext.cs` |

### Step 7: Trace the Layer Boundaries

For the DraftToAssigned flow, identify where each architectural boundary is crossed:

1. **UI → Core:** The API controller sends a command defined in Core
2. **Core → DataAccess:** MediatR routes the command to a handler in DataAccess
3. **DataAccess → Database:** The handler uses DataContext to persist changes

Note that the **command object itself** lives in Core, while the **handler** lives in DataAccess. This is the key insight of Onion Architecture with CQRS.

### Step 8: Review All Workflow Diagrams

Browse the other workflow diagrams in `arch/`:
- `WorflowForSaveDraftCommand.md`
- `WorflowForAssignedToInProgressCommand.md`
- `WorflowForInProgressToCompleteCommand.md`

Notice the consistent pattern: every request flows UI → IBus → MediatR → Handler → Database.

---

## Expected Outcome

- A hand-drawn (or digital) architecture diagram with file paths annotated
- Understanding of why Core has no outward dependencies
- Ability to trace any request from UI to database

---

## Discussion Questions

1. How does the Onion Architecture enforce "dependency flow inward"? What mechanism prevents violations?
2. Why are command **definitions** in Core but command **handlers** in DataAccess?
3. How does this architecture relate to the "Create Clarity" pillar? Does the structure make the system easier or harder to understand?
4. What would happen to productivity if the architecture rules were violated and any layer could reference any other?
5. How does this map to the "Three Ways of DevOps" — particularly Flow (optimizing work from dev to production)?
