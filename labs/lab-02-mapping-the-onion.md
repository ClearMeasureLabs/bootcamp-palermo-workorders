# Lab 02: Mapping the Onion - Architecture Exploration

**Curriculum Section:** Sections 01-02 (Model of Software Leadership / Structure of a Software Project)
**Estimated Time:** 45 minutes
**Type:** Analyze / Observe

---

## Objective

Trace the Onion Architecture layers and dependency rules in the live codebase. Understand how architecture creates clarity (Pillar 1) and establishes quality (Pillar 2).

---

## Steps

### Step 1: Study the C4 Component Diagram

Open `arch/arch-c4-component-project-dependencies.md`. Sketch the dependency arrows on paper.

### Step 2: Verify Core Has No Project References

Open `src/Core/Core.csproj`. Confirm there are no `<ProjectReference>` elements. Core depends on nothing — only abstraction NuGet packages.

### Step 3: Verify DataAccess References Only Core

Open `src/DataAccess/DataAccess.csproj`. The only project reference is `../Core/Core.csproj`.

### Step 4: Examine the Outer Layer

Open `src/UI/Server/UI.Server.csproj`. This composition root references Core, DataAccess, UI.Client, UI.Api, LlmGateway, McpServer, Worker — the outermost layer wires everything together.

### Step 5: Attempt an Architecture Violation

Add `using ClearMeasure.Bootcamp.DataAccess;` to any Core file. Build:

```powershell
dotnet build src/Core/Core.csproj
```

Observe the failure. **Revert immediately.**

### Step 6: Trace a Full Request

Open `arch/WorflowForDraftToAssignedCommand.md`. Map each participant to its source file:

| Diagram Participant | Source File |
|---------------------|-------------|
| `SingleApiController` | `src/UI/Api/Controllers/SingleApiController.cs` |
| `Bus : IBus` | `src/Core/IBus.cs` (interface) |
| `DraftToAssignedCommand` | `src/Core/Model/StateCommands/DraftToAssignedCommand.cs` |
| `StateCommandHandler` | `src/DataAccess/Handlers/StateCommandHandler.cs` |
| `DataContext` | `src/DataAccess/Mappings/DataContext.cs` |

### Step 7: Trace the Layer Boundaries

Identify where each architectural boundary is crossed:
1. **UI → Core:** API controller sends a command defined in Core
2. **Core → DataAccess:** MediatR routes the command to a handler in DataAccess
3. **DataAccess → Database:** Handler uses DataContext to persist

The **command object** lives in Core; the **handler** lives in DataAccess — the key insight of Onion Architecture with CQRS.

### Step 8: Review All Workflow Diagrams

Browse `arch/WorflowForSaveDraftCommand.md`, `arch/WorflowForAssignedToInProgressCommand.md`, `arch/WorflowForInProgressToCompleteCommand.md`. Note the consistent pattern.

---

## Expected Outcome

- Hand-drawn architecture diagram with file paths annotated
- Understanding of why Core has no outward dependencies

---

## Discussion Questions

1. How does the Onion Architecture enforce "dependency flow inward"?
2. Why are command definitions in Core but command handlers in DataAccess?
3. How does this map to the "Three Ways of DevOps" — particularly Flow?
