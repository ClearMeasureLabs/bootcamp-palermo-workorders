# Work Order Management System

A work order management application built with .NET 10.0 implementing Onion Architecture. The system uses Blazor WebAssembly for the UI, Entity Framework Core for data access, MediatR for CQRS, and deploys to Azure Container Apps.

This codebase serves as both a working application and a teaching reference for software architecture. The 51 architectural patterns cataloged below are all demonstrated in the source code.

## Solution Structure

```
src/
  Core/                  Domain layer — models, interfaces, queries (no dependencies)
  DataAccess/            EF Core, MediatR handlers (references Core only)
  Database/              DbUp schema migrations
  UI/Server/             Blazor Server host, Lamar DI
  UI/Client/             Blazor WebAssembly frontend
  UI/Api/                Web API endpoints
  UI.Shared/             Shared UI types
  LlmGateway/            Azure OpenAI integration
  Worker/                Background hosted service
  ChurchBulletin.AppHost/         .NET Aspire orchestration
  ChurchBulletin.ServiceDefaults/ Aspire service defaults
  UnitTests/             NUnit + Shouldly
  IntegrationTests/      NUnit, LocalDB / SQL Server / SQLite
  AcceptanceTests/       NUnit + Playwright
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- One of the following database options:
  - **Windows:** SQL Server LocalDB (included with Visual Studio)
  - **Linux/macOS with Docker:** SQL Server 2022 runs automatically in a container
  - **Linux/macOS without Docker:** SQLite (automatic fallback)
- [PowerShell 7+](https://github.com/PowerShell/PowerShell) (cross-platform, required for build scripts)
- [Playwright browsers](https://playwright.dev/) (for acceptance tests only)

## Build

```powershell
# Quick build (Windows)
.\build.bat

# Quick build (Linux/macOS)
./build.sh

# Full private build — clean, compile, unit tests, DB migration, integration tests
. .\build.ps1 ; Invoke-PrivateBuild

# CI build — private build + packaging
. .\build.ps1 ; Invoke-CIBuild

# dotnet CLI directly
dotnet build src/ChurchBulletin.sln --configuration Release
```

## Run Tests

```powershell
# Unit tests
dotnet test src/UnitTests --configuration Release

# Integration tests
dotnet test src/IntegrationTests --configuration Release

# Acceptance tests (install Playwright browsers first)
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
dotnet test src/AcceptanceTests --configuration Debug
```

## Run Locally

```bash
cd src/UI/Server
dotnet run
```

The application starts at `https://localhost:7174`. Health check endpoint: `https://localhost:7174/_healthcheck`.

---

# Architecture Patterns Reference

A catalog of 51 architectural patterns and design concepts demonstrated in this codebase, annotated with authoritative reference URLs suitable for student learning.

---

## Domain & Application Architecture

### 1. Onion Architecture
Layered architecture where dependencies point inward. Inner layers define interfaces; outer layers implement them. Core has zero outward dependencies.
- **Reference:** [Jeffrey Palermo — The Onion Architecture, Part 1 (2008)](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)

### 2. CQRS (Command Query Responsibility Segregation)
Separates read models (queries) from write models (commands), allowing each to be optimized independently.
- **Reference:** [Martin Fowler — CQRS](https://www.martinfowler.com/bliki/CQRS.html)

### 3. Mediator Pattern
A behavioral design pattern where a mediator object encapsulates how objects interact, promoting loose coupling. Implemented here via the MediatR library.
- **Reference:** [Refactoring Guru — Mediator](https://refactoring.guru/design-patterns/mediator)

### 4. Domain Model
An object model of the business domain that incorporates both behavior and data. Business logic lives inside the domain objects themselves.
- **Reference:** [Martin Fowler — Domain Model (P of EAA)](https://martinfowler.com/eaaCatalog/domainModel.html)

### 5. Layer Supertype (Entity Base Class)
An abstract base class that provides common behavior (identity, equality) for all domain entities in a layer.
- **Reference:** [Martin Fowler — Layer Supertype (P of EAA)](https://martinfowler.com/eaaCatalog/layerSupertype.html)

### 6. Value Object
An immutable object defined entirely by its attributes rather than a unique identity. Two value objects with the same attributes are considered equal.
- **Reference:** [Martin Fowler — Value Object](https://www.martinfowler.com/bliki/ValueObject.html)

### 7. Enumeration Class (Smart Enum)
Replaces primitive `enum` types with full classes that carry behavior, enabling richer domain modeling and avoiding primitive obsession.
- **Reference:** [Microsoft — Enumeration Classes over Enum Types](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types)

### 8. State Machine Pattern
Objects transition through a set of defined states via validated transitions, preventing invalid state changes.
- **Reference:** [Refactoring Guru — State Pattern](https://refactoring.guru/design-patterns/state)

### 9. Repository Pattern
Abstracts data access behind a collection-like interface, decoupling the domain from persistence technology. Implemented implicitly via EF Core's `DbContext`.
- **Reference:** [Martin Fowler — Repository (P of EAA)](https://martinfowler.com/eaaCatalog/repository.html)

### 10. Unit of Work
Tracks all changes made during a business transaction and commits them as a single atomic operation. Implemented implicitly via EF Core's `SaveChanges`.
- **Reference:** [Martin Fowler — Unit of Work (P of EAA)](https://martinfowler.com/eaaCatalog/unitOfWork.html)

---

## Dependency Management & Wiring

### 11. Dependency Injection / Inversion of Control
Components declare their dependencies through constructor parameters; an external container resolves and provides them at runtime.
- **Reference:** [Martin Fowler — Inversion of Control Containers and the Dependency Injection Pattern](https://martinfowler.com/articles/injection.html)

### 12. Service Registry
A well-known object that other objects use to find common services. Centralized registration of services in a single registry class.
- **Reference:** [Martin Fowler — Registry (P of EAA)](https://martinfowler.com/eaaCatalog/registry.html)

### 13. Convention over Configuration
The framework automatically discovers and registers services by scanning assemblies, reducing explicit configuration in favor of naming and structural conventions.
- **Reference:** [Wikipedia — Convention over Configuration](https://en.wikipedia.org/wiki/Convention_over_configuration)

---

## UI & Presentation Architecture

### 14. Blazor WebAssembly (Client-Side SPA)
A single-page application framework that runs .NET code directly in the browser via WebAssembly, eliminating the need for JavaScript.
- **Reference:** [Microsoft Learn — ASP.NET Core Blazor Hosting Models](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)

### 15. Server-Side Rendering with Client Hydration
Initial rendering occurs on the server for fast first paint; the client-side framework then takes over for interactivity.
- **Reference:** [Microsoft Learn — ASP.NET Core Blazor Render Modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)

### 16. Backend for Frontend (BFF)
A dedicated API layer tailored to the needs of a specific frontend, rather than a generic API serving all consumers.
- **Reference:** [Azure Architecture Center — Backends for Frontends Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends)

### 17. Shared Kernel
A shared set of types and interfaces used across bounded contexts or UI boundaries, maintained as a single shared project.
- **Reference:** [Wikipedia — Domain-Driven Design (Shared Kernel)](https://en.wikipedia.org/wiki/Domain-driven_design)

---

## Data & Persistence

### 18. Evolutionary Database Design (Database Migrations)
Incremental, versioned schema changes applied via numbered migration scripts, allowing the database to evolve alongside application code.
- **Reference:** [Martin Fowler — Evolutionary Database Design](https://martinfowler.com/articles/evodb.html)

---

## Infrastructure & Deployment

### 19. Containerization
Packaging an application and its dependencies into a lightweight, portable container image that runs consistently across environments.
- **Reference:** [Docker — What is a Container?](https://www.docker.com/resources/what-container/)

### 20. Container Orchestration
A managed runtime that handles container deployment, scaling, networking, and revision management.
- **Reference:** [Microsoft Learn — Azure Container Apps Overview](https://learn.microsoft.com/en-us/azure/container-apps/overview)

### 21. Continuous Integration
Automatically building and testing every code change when pushed, providing rapid feedback on integration errors.
- **Reference:** [Martin Fowler — Continuous Integration](https://martinfowler.com/articles/continuousIntegration.html)

### 22. Continuous Delivery
An automated pipeline that ensures code is always in a deployable state, from commit through testing environments to production.
- **Reference:** [Martin Fowler — Continuous Delivery](https://martinfowler.com/bliki/ContinuousDelivery.html)

### 23. Environment Parity (Dev/Prod Parity)
Keeping development, staging, and production environments as similar as possible to reduce deployment surprises.
- **Reference:** [The Twelve-Factor App — X. Dev/Prod Parity](https://12factor.net/dev-prod-parity)

### 24. Blue-Green / Revision-Based Deployment
Deploying a new version alongside the old one, then switching traffic, enabling zero-downtime releases and easy rollback.
- **Reference:** [Martin Fowler — Blue Green Deployment](https://martinfowler.com/bliki/BlueGreenDeployment.html)

### 25. Strangler Fig (Dual Pipeline)
Incrementally replacing a legacy system by routing functionality to a new implementation while the old one is gradually retired. Applied here to the migration from Azure DevOps to GitHub Actions.
- **Reference:** [Martin Fowler — Strangler Fig Application](https://martinfowler.com/bliki/StranglerFigApplication.html)

### 26. Infrastructure as Code
Defining infrastructure declaratively in version-controlled files (ARM templates, pipeline YAML, Octopus OCL) rather than through manual configuration.
- **Reference:** [Wikipedia — Infrastructure as Code](https://en.wikipedia.org/wiki/Infrastructure_as_code)

### 27. Configuration as Code
Deployment processes, variables, and settings stored as code in the repository, enabling versioning, diffing, and review of deployment configuration.
- **Reference:** [Octopus Deploy — Deployment Process as Code](https://octopus.com/docs/deployments/patterns/deployment-process-as-code)

### 28. Semantic Versioning
A versioning scheme (`MAJOR.MINOR.PATCH`) with defined rules about when each number increments, communicating the nature of changes to consumers.
- **Reference:** [Semantic Versioning 2.0.0](https://semver.org/)

### 29. Immutable Build Artifacts
The same compiled artifact flows unchanged through all environments (TDD, UAT, Prod). What was tested is what gets deployed.
- **Reference:** [Minimum CD — Immutable Artifacts](https://minimumcd.org/minimumcd/immutable/)

### 30. Approval Gates / Environment Promotion
Requiring manual reviewer approval before deploying to higher environments, enforcing governance over the release pipeline.
- **Reference:** [Microsoft Learn — Gates and Approvals](https://learn.microsoft.com/en-us/azure/devops/pipelines/release/approvals/gates)

### 31. Release Management
Separating the concerns of "build" from "deploy" with a dedicated release management tool that tracks what version is in which environment.
- **Reference:** [Octopus Deploy — Releases and Deployments](https://octopus.com/docs/best-practices/deployments/releases-and-deployments)

---

## Health & Observability

### 32. Health Endpoint Monitoring Pattern
Dedicated endpoints that report system health, enabling load balancers, orchestrators, and deployment pipelines to verify application readiness.
- **Reference:** [Azure Architecture Center — Health Endpoint Monitoring Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/health-endpoint-monitoring)

### 33. Health Check Gating (Deployment Verification)
Blocking deployment progression (e.g., acceptance test execution) until health endpoints confirm the application is ready.
- **Reference:** [Azure Architecture Center — Health Endpoint Monitoring Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/health-endpoint-monitoring)

### 34. Client-Side Health Checks (Distributed Health Monitoring)
Health checks executed from the client perspective (Blazor WASM) back to the server, providing end-to-end health visibility beyond server-side checks alone.
- **Reference:** [Microsoft Learn — ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

## Testing Patterns

### 35. Test Pyramid
A layered testing strategy with many fast unit tests at the base, fewer integration tests in the middle, and a small number of end-to-end acceptance tests at the top.
- **Reference:** [Martin Fowler — Test Pyramid](https://martinfowler.com/bliki/TestPyramid.html)

### 36. Test Double (Stub Pattern)
Replacing real dependencies with simplified implementations ("stubs") during testing. This codebase uses the "Stub" prefix convention rather than "Mock."
- **Reference:** [Martin Fowler — Test Double](https://martinfowler.com/bliki/TestDouble.html)

### 37. Test Data Builder
Generating test data with fluent builder objects (via AutoBogus), producing valid domain objects without verbose manual construction.
- **Reference:** [Nat Pryce — Test Data Builders: An Alternative to the Object Mother Pattern](http://www.natpryce.com/articles/000714.html)

### 38. Arrange-Act-Assert (AAA)
Structuring each test method into three phases: set up preconditions, execute the action under test, and verify the result.
- **Reference:** [C2 Wiki — Arrange Act Assert](https://wiki.c2.com/?ArrangeActAssert)

### 39. Acceptance Testing with Browser Automation
End-to-end tests that drive a real browser to verify the application behaves correctly from the user's perspective.
- **Reference:** [Playwright Documentation](https://playwright.dev/)

### 40. Component Testing (bUnit)
Unit testing individual UI components in isolation by rendering them in a test host without a browser.
- **Reference:** [bUnit — A Testing Library for Blazor Components](https://bunit.dev/)

### 41. Multi-Database Engine Testing
Running the same test suite against multiple database engines (SQL Server, SQLite, LocalDB) to validate that data access logic is not vendor-specific.
- **Reference:** [Microsoft Learn — Choosing a Testing Strategy (EF Core)](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)

### 42. Multi-Architecture CI (Platform Matrix Testing)
Running CI builds in parallel across multiple CPU architectures (x86_64, ARM64) to verify cross-platform compatibility.
- **Reference:** [GitHub Docs — Running Variations of Jobs in a Workflow](https://docs.github.com/actions/writing-workflows/choosing-what-your-workflow-does/running-variations-of-jobs-in-a-workflow)

---

## Design Patterns (GoF & Other)

### 43. Factory Pattern
Creates objects without exposing the instantiation logic, letting subclasses or configuration determine which concrete class to create.
- **Reference:** [Refactoring Guru — Factory Method](https://refactoring.guru/design-patterns/factory-method)

---

## .NET Platform Patterns

### 44. Hosted Service / Background Worker
Long-running background tasks implemented as hosted services within the .NET generic host, running alongside the web application.
- **Reference:** [Microsoft Learn — Worker Services in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers)

### 45. .NET Aspire (Service Orchestration)
Development-time orchestration of distributed services, wiring connection strings, health checks, and service discovery for local development.
- **Reference:** [Microsoft Learn — .NET Aspire App Host Overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview)

### 46. Nullable Reference Types (Compile-Time Null Safety)
Static analysis annotations that help the compiler detect potential null reference errors at compile time rather than runtime.
- **Reference:** [Microsoft Learn — Nullable Reference Types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)

### 47. NuGet Package as Deployment Artifact
Using the NuGet package format not just for library distribution but as the deployment unit that flows through the release pipeline.
- **Reference:** [Microsoft Learn — What is NuGet?](https://learn.microsoft.com/en-us/nuget/what-is-nuget)

---

## AI / LLM Integration

### 48. AI/LLM Gateway Pattern
An abstraction layer between the application and AI services, encapsulating connection management, health checks, and model configuration.
- **Reference:** [Azure Architecture Center — Azure OpenAI Gateway Guide](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/azure-openai-gateway-guide)

---

## Architecture Documentation

### 49. C4 Model
Documenting software architecture at four hierarchical levels of abstraction: System Context, Container, Component, and Code (Class).
- **Reference:** [The C4 Model for Visualising Software Architecture](https://c4model.com/)

### 50. 4+1 Architectural View Model
Describing architecture from five concurrent viewpoints: Logical, Development, Process, Physical, and Scenarios — each addressing different stakeholder concerns.
- **Reference:** [Wikipedia — 4+1 Architectural View Model](https://en.wikipedia.org/wiki/4%2B1_architectural_view_model)

### 51. Architecture Documentation as Code
Maintaining architecture diagrams (PlantUML, Mermaid) as source files checked into version control alongside application code, enabling versioning, diffing, and review.
- **Reference:** [The C4 Model — Tooling](https://c4model.com/#Tooling)
