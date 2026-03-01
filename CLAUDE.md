# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Work Order management system. .NET 10.0, Onion Architecture, Blazor WebAssembly + Server UI, EF Core 10, SQL Server, MediatR for CQRS, Lamar DI, deployed to Azure Container Apps.

**Solution:** `src/ChurchBulletin.sln`

## Build and Test

```powershell
# Full local build (compile + unit tests + DB migration + integration tests)
. .\build.ps1 ; Build

# Build only
dotnet build src/ChurchBulletin.sln --configuration Release

# Unit tests
dotnet test src/UnitTests --configuration Release

# Single unit test by name
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Integration tests (requires SQL Server — LocalDB on Windows, Docker container on Linux, SQLite fallback)
dotnet test src/IntegrationTests --configuration Release

# Acceptance tests (Playwright — install browsers first)
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
dotnet test src/AcceptanceTests --configuration Debug

# Single acceptance test
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

**Run locally:** `cd src/UI/Server && dotnet run` → `https://localhost:7174` (health: `/_healthcheck`)

## Onion Architecture (Strict)

Dependency flow is inward only. Violations will break the build.

- **Core** (`src/Core/`) → NO project references. Domain models, interfaces, query objects.
- **DataAccess** (`src/DataAccess/`) → references Core only. EF Core context, MediatR handlers.
- **UI layer** (`src/UI/Server/`, `src/UI/Client/`, `src/UI/Api/`, `src/UI.Shared/`) → outer layer.
- **Database** (`src/Database/`) → DbUp migrations, independent of application layers.

## Request Flow (CQRS)

Every operation flows through MediatR via the `IBus` abstraction:

```
User → Blazor UI → API Controller → IBus.Send(query/command)
  → MediatR → Handler (in DataAccess/) → DataContext (EF Core) → SQL Server
```

**Queries:** Defined in `src/Core/Queries/` (e.g., `WorkOrderByNumberQuery`). Handlers in `src/DataAccess/Handlers/`.

**State Commands:** Defined in `src/Core/Model/StateCommands/`. Each command implements `IStateCommand` and mutates the domain model. Flow: `StateCommandHandler` → `command.Execute(workOrder)` → `DataContext.SaveChangesAsync()`.

Work order state transitions: Draft → Assigned → InProgress → Complete (also Cancelled from any state). See `arch/WorflowFor*.md` for sequence diagrams.

## Domain Model

- **WorkOrder**: Number, Title, Description, RoomNumber, Status (WorkOrderStatus), Creator/Assignee (Employee), AssignedDate, CreatedDate, CompletedDate. Methods: `ChangeStatus()`, `CanReassign()`
- **Employee**: UserName, FirstName, LastName, EmailAddress, Roles. Methods: `CanCreateWorkOrder()`, `CanFulfilWorkOrder()`
- **WorkOrderStatus**: Smart enum — Draft, Assigned, InProgress, Complete, Cancelled. Factory methods: `FromCode()`, `FromKey()`
- **Role**: Name, CanCreateWorkOrder, CanFulfillWorkOrder

## Database Migrations

DbUp scripts in `src/Database/scripts/Update/`, numbered sequentially (`###_Description.sql`).
- Use TABS for indentation in SQL scripts.
- To add a migration: find the highest existing number, increment by 1.
- Apply locally by running `Build`.

## Key Conventions

**Architecture rules (violations are auto-rejected in PR review):**
- Do NOT add NuGet packages or change SDK versions without explicit approval
- Do NOT modify `.octopus/`, build scripts, or pipeline files without approval
- Strictly maintain onion dependency rules

**Testing:**
- Framework: NUnit 4.x with Shouldly assertions (NOT FluentAssertions, NOT Assert.That)
- Test doubles: prefix with `Stub` (NOT `Mock`)
- Pattern: AAA (Arrange, Act, Assert) without section comments
- Test naming: `[MethodName]_[Scenario]_[ExpectedResult]`, prefixed with `Should` or `When`
- Test data generation: AutoBogus
- UI component tests: bUnit
- Acceptance tests: Playwright with helpers from `AcceptanceTestBase` (`LoginAsCurrentUser()`, `Click()`, `Input()`, `Select()`)

**Code style:**
- PascalCase for classes/methods, camelCase for variables
- XML documentation on public APIs
- Nullable reference types enabled

**Response style:**
- No anthropomorphizing — no "I", "me", "you", "we", "us"
- Terse, direct statements. Say "Checking this file" not "Let me check this file"

## Dependencies

**.NET SDK:** 10.0.100 (see `global.json`)

**Core** — MediatR.Contracts 2.0.1, Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.0, Microsoft.Extensions.Logging.Abstractions 10.0.0

**DataAccess** — MediatR 12.4.1, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0, NServiceBus.Persistence.Sql.TransactionalSession 8.3.0, ClearMeasureLabs.HostedEndpoint.SqlServerTransport 1.0.30

**Database** — DbUp 5.0.41, dbup-sqlserver 6.0.16, Spectre.Console 0.54.0, Spectre.Console.Cli 0.53.0

**UI.Server** — Lamar.Microsoft.DependencyInjection 15.0.1, Azure.Monitor.OpenTelemetry.AspNetCore 1.3.0, Microsoft.ApplicationInsights.AspNetCore 2.23.0, Microsoft.AspNetCore.Components.WebAssembly.Server 10.0.0, ModelContextProtocol 1.0.0, ModelContextProtocol.AspNetCore 1.0.0, NServiceBus.Extensions.Hosting 3.0.1, OpenTelemetry 1.12.0

**UI.Client** — BlazorApplicationInsights 3.2.1, BlazorMvc 2.1.1, MediatR 12.4.1, Lamar.Microsoft.DependencyInjection 15.0.1, Microsoft.AspNetCore.Components.WebAssembly 10.0.0

**UI.Api** — Lamar.Microsoft.DependencyInjection 15.0.1

**UI.Shared** — BlazorApplicationInsights 3.2.1, BlazorMvc 2.1.1, MediatR 12.4.1, Microsoft.ApplicationInsights 2.23.0, Microsoft.AspNetCore.Components.Authorization 10.0.0

**LlmGateway** — Azure.AI.OpenAI 2.1.0, MediatR 12.4.1, Microsoft.Extensions.AI 9.7.0, Microsoft.Extensions.AI.OpenAI 9.7.1-preview.1.25365.4, Microsoft.Extensions.AI.Abstractions 9.7.1

**McpServer** — ModelContextProtocol 1.0.0, ModelContextProtocol.AspNetCore 1.0.0, Lamar.Microsoft.DependencyInjection 15.0.1, MediatR 12.4.1, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0

**Worker** — ClearMeasureLabs.HostedEndpoint.SqlServerTransport 1.0.30, Microsoft.Extensions.Hosting 10.0.2

**AppHost** — Aspire.Hosting.AppHost 9.5.0

**ServiceDefaults** — Azure.Monitor.OpenTelemetry.AspNetCore 1.3.0, OpenTelemetry.Exporter.OpenTelemetryProtocol 1.12.0, OpenTelemetry.Extensions.Hosting 1.12.0, Microsoft.Extensions.Http.Resilience 9.9.0, Microsoft.Extensions.ServiceDiscovery 9.5.0

**UnitTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, Shouldly 4.3.0, bunit 1.40.0, AutoBogus.Conventions 2.13.1, MediatR 12.4.1, coverlet.msbuild 6.0.4

**IntegrationTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, Shouldly 4.3.0, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.Extensions.Hosting 10.0.0, coverlet.msbuild 6.0.4

**AcceptanceTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, microsoft.playwright.nunit 1.54.0, Azure.AI.OpenAI 2.1.0, ModelContextProtocol 1.0.0, Microsoft.Extensions.AI 9.7.0, Microsoft.Extensions.AI.OpenAI 9.7.1-preview.1.25365.4

## DI and Service Wiring

Lamar container configured in `src/UI/Server/UIServiceRegistry.cs`. Assembly scanning auto-registers MediatR handlers and services. The `IBus` interface wraps MediatR's `IMediator`.

## Branch Naming

Format: `{username}/{branch-description}`. AI agents use the username of the account that initiated the session. When on a branch, add/commit/push automatically without asking.

## Quality Gates

| When | Command |
|------|---------|
| Before commit | `.\privatebuild.ps1` (or `. .\build.ps1 ; Build`) |
| Before PR | `.\acceptancetests.ps1` |
| Docs-only changes | Skip builds |

## Further Reference

- Architecture diagrams: `arch/` (C4 PlantUML + Mermaid)
- Copilot standards: `.github/copilot-instructions.md`
- PR review rules: `.github/copilot-code-review-instructions.md`
- Codebase structure: `.cursor/rules/codebase-structure.mdc`
