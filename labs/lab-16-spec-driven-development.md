# Lab 16: Spec-Driven Development with OpenSpec

**Curriculum Section:** Section 08 (AI-Driven Development)
**Estimated Time:** 45 minutes
**Type:** Build + Experiment

---

## Objective

Practice spec-driven development where a structured specification drives unattended feature development by an AI agent. Write a specification, hand it to an AI coding agent, and evaluate the output without intervening.

---

## Context

Spec-driven development inverts the traditional workflow: instead of writing code and then verifying it meets requirements, you write a precise specification and let an AI agent generate the implementation. The specification becomes the contract; the agent becomes the implementer. The developer's role shifts from "writer" to "architect + reviewer."

The key insight: **the quality of the specification determines the quality of the output.**

---

## Steps

### Step 1: Study the Existing Architecture Specs

Review the architecture documentation that already serves as specifications:
- `arch/WorflowForDraftToAssignedCommand.md` — sequence diagram as spec
- `arch/arch-c4-component-project-dependencies.md` — dependency rules as spec
- `CLAUDE.md` — coding standards as spec

These documents constrain what the AI agent can do. A good spec narrows the solution space.

### Step 2: Write a Feature Specification

Write a spec for this feature:

> **Feature: Work Order Instructions Field**
>
> **Domain Change:**
> - Add an `Instructions` property to `WorkOrder` (type: `string?`, nullable, no default value)
>
> **Database:**
> - Add migration: `ALTER TABLE dbo.WorkOrder ADD Instructions NVARCHAR(4000) NULL`
> - Use next sequential migration number after existing scripts in `src/Database/scripts/Update/`
>
> **EF Core Mapping:**
> - Map `Instructions` in `WorkOrderMap.cs` with `HasMaxLength(4000)`
>
> **Unit Tests (src/UnitTests):**
> - `Instructions_WhenNotSet_ShouldBeNull`
> - `Instructions_WhenSet_ShouldRetainValue`
>
> **Integration Test (src/IntegrationTests):**
> - Save a work order with Instructions text, read it back, verify persistence
>
> **Constraints:**
> - No new NuGet packages
> - Follow Shouldly assertion convention
> - Follow AAA pattern without section comments
> - Maintain onion architecture (Core has no project references)

### Step 3: Hand the Spec to an AI Agent

Give the specification to Claude Code or your AI coding tool. Do NOT provide additional guidance — the spec should be self-contained.

### Step 4: Do Not Intervene

Let the agent work unattended. This is the "unattended feature development" concept. Note:
- How long does it take?
- Does it ask questions or proceed directly?
- Does it run the build?

### Step 5: Evaluate the Output

Review the generated code against the specification:

| Spec Requirement | Implemented? | Correct? |
|------------------|-------------|----------|
| Property added to `WorkOrder.cs` in Core | | |
| Property is `string?` with no default | | |
| Migration script with correct number | | |
| EF mapping with `HasMaxLength(4000)` | | |
| Unit test for null default | | |
| Unit test for explicit value | | |
| Integration test for persistence | | |
| No new NuGet packages | | |
| Shouldly assertions used | | |
| `PrivateBuild.ps1` passes | | |

### Step 6: Refine the Spec

If the output was incorrect, identify which specification was ambiguous or missing. Rewrite the spec to be more precise and try again.

**Common spec failures:**
- Ambiguous location: "Add a test" (where? which project? which file?)
- Missing convention: "Write a test" (which assertion library? naming pattern?)
- Missing constraint: "Add a field" (nullable? default? max length?)

### Step 7: Compare Spec Quality to Output Quality

Run the experiment twice:
1. **Vague spec:** "Add an Instructions field to work orders with tests"
2. **Precise spec:** The full specification from Step 2

Compare the outputs. The precise spec should produce significantly better results.

---

## Expected Outcome

- A feature implemented entirely from a written specification
- Understanding of how spec precision affects output quality
- A refined specification that produces correct, convention-following code
