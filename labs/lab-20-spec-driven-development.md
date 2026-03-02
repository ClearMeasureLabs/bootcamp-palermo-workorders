# Lab 20: Spec-Driven Development with OpenSpec

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

> **Feature: Work Order Priority Field**
>
> **Domain Change:**
> - Add a `Priority` property to `WorkOrder` (type: `string?`, default: `"Normal"`)
> - Valid values: `"Low"`, `"Normal"`, `"High"`, `"Urgent"`
>
> **Database:**
> - Add migration: `ALTER TABLE dbo.WorkOrder ADD Priority NVARCHAR(20) NULL DEFAULT 'Normal'`
> - Use next sequential migration number after existing scripts in `src/Database/scripts/Update/`
>
> **EF Core Mapping:**
> - Map `Priority` in `WorkOrderMap.cs` with `HasMaxLength(20)`
>
> **Unit Tests (src/UnitTests):**
> - `Priority_WhenNotSet_ShouldDefaultToNormal`
> - `Priority_WhenSetToUrgent_ShouldRetainValue`
>
> **Integration Test (src/IntegrationTests):**
> - Save a work order with Priority "Urgent", read it back, verify persistence
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
| Default value is `"Normal"` | | |
| Migration script with correct number | | |
| EF mapping with `HasMaxLength(20)` | | |
| Unit test for default value | | |
| Unit test for explicit value | | |
| Integration test for persistence | | |
| No new NuGet packages | | |
| Shouldly assertions used | | |
| `privatebuild.ps1` passes | | |

### Step 6: Refine the Spec

If the output was incorrect, identify which specification was ambiguous or missing. Rewrite the spec to be more precise and try again.

**Common spec failures:**
- Ambiguous location: "Add a test" (where? which project? which file?)
- Missing convention: "Write a test" (which assertion library? naming pattern?)
- Missing constraint: "Add a field" (nullable? default? max length?)

### Step 7: Compare Spec Quality to Output Quality

Run the experiment twice:
1. **Vague spec:** "Add a Priority field to work orders with tests"
2. **Precise spec:** The full specification from Step 2

Compare the outputs. The precise spec should produce significantly better results.

---

## Expected Outcome

- A feature implemented entirely from a written specification
- Understanding of how spec precision affects output quality
- A refined specification that produces correct, convention-following code

---

## Discussion Questions

1. How much time did you spend writing the spec vs. how much time would you spend writing the code manually? At what team size does spec-driven development become more efficient?
2. The spec references specific file paths and conventions. Why is this level of detail necessary for AI agents?
3. What happens when the spec contradicts the `CLAUDE.md` guardrails? Which wins?
4. Could this spec be used as an acceptance criterion in a GitHub issue? How would you combine spec-driven development with issue tracking?
5. The curriculum mentions "Extreme Agile — AI-Driven Development." How does spec-driven development change the sprint planning process?
6. What types of specifications are easy to write? What types are hard? (Easy: data model changes. Hard: complex UI interactions, performance requirements)
