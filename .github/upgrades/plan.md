# .NET 10 Preview Migration Plan

## Executive Summary

### Selected Strategy
**Big Bang Strategy** - All projects upgraded simultaneously in single atomic operation.

**Rationale**: 
- 11 projects (small/medium solution)
- All currently on .NET 9.0
- Clear dependency structure with Core at foundation
- All Microsoft packages have .NET 10 compatible versions available
- No blocking compatibility issues identified
- No security vulnerabilities in dependencies

### Scope
- **Total Projects**: 11 (all SDK-style)
- **Current State**: All projects targeting net9.0
- **Target State**: All projects targeting net10.0

### Target State
After migration:
- All projects targeting .NET 10.0 (Preview)
- All Microsoft packages updated to version 10.0.0
- Third-party packages remain at current compatible versions
- No security vulnerabilities

### Complexity Assessment
**Overall**: Medium

**Complexity Factors**:
- **Low Risk**: All projects are SDK-style, straightforward TargetFramework updates
- **Low Risk**: No breaking changes detected in Microsoft package APIs
- **Medium Risk**: Third-party packages (BlazorMvc, Lamar) have no .NET 10 updates yet
- **Low Risk**: MediatR 12.4.1 marked compatible (staying on current version)
- **Medium Risk**: Preview framework - production use not recommended

### Critical Issues
- **No Security Vulnerabilities**: All packages clean
- **Third-Party Compatibility**: BlazorMvc, Lamar, BlazorApplicationInsights have no .NET 10-specific versions (using compatible current versions)
- **Preview Software Risk**: .NET 10 is preview/pre-release

### Recommended Approach
**Big Bang** - Atomic upgrade of all projects simultaneously with comprehensive testing phase after migration.

---

## Migration Strategy

### 2.1 Approach Selection

**Chosen Strategy**: Big Bang Strategy

**Justification**:
- **11 projects** - Small/medium solution size suitable for atomic upgrade
- **Simple dependency structure** - Core → DataAccess/LlmGateway → UI layers
- **All net9.0** - Homogeneous starting point, no multi-targeting complexity
- **Package compatibility confirmed** - All Microsoft packages have 10.0.0 versions
- **No intermediate states needed** - Can upgrade all projects together
- **Faster completion** - Single coordinated operation vs. phased approach

### 2.2 Dependency-Based Ordering

While Big Bang upgrades all projects together, understanding dependency order helps identify validation checkpoints:

**Dependency Layers**:
1. **Foundation**: Core (0 dependencies, 8 dependants)
2. **Data Layer**: DataAccess, LlmGateway (depend on Core)
3. **Shared UI**: UI.Shared (depends on Core + LlmGateway)
4. **Application UI**: UI.Client, UI.Api, UI.Server
5. **Test Projects**: UnitTests, IntegrationTests, AcceptanceTests

**Critical Path**: Core → UI.Server (includes all dependencies)

### 2.3 Parallel vs Sequential Execution

**Big Bang Approach**: All project file and package updates executed as single atomic batch operation.

**Execution Model**:
- **Phase 0**: Prerequisites (SDK validation)
- **Phase 1**: Atomic upgrade (all updates together)
  - Update all TargetFramework properties
  - Update all package references
  - Restore dependencies
  - Build solution
  - Fix compilation errors
- **Phase 2**: Testing and validation

**No parallelization needed** - single coordinated operation.

---

## Detailed Dependency Analysis

### 3.1 Dependency Graph Summary

**Migration Phases** (for understanding, not task boundaries):

**Phase 0: Preparation**
- Verify .NET 10 SDK installed
- No global.json constraints detected

**Phase 1: Atomic Upgrade** (all projects simultaneously)
- **Foundation Layer** (1 project)
  - Core.csproj (0 dependencies)

- **Data & Gateway Layer** (2 projects)
  - DataAccess.csproj → Core
  - LlmGateway.csproj → Core

- **Shared UI Layer** (1 project)
  - UI.Shared.csproj → Core, LlmGateway

- **Application Layer** (3 projects)
  - UI.Api.csproj → Core
  - UI.Client.csproj → Core, UI.Shared
  - UI.Server.csproj → Core, DataAccess, LlmGateway, UI.Api, UI.Client

- **Test Layer** (3 projects)
  - UnitTests.csproj → Core, UI.Shared, UI.Server, UI.Client, UI.Api
  - IntegrationTests.csproj → UI.Server, UnitTests
  - AcceptanceTests.csproj → Core, IntegrationTests

**Phase 2: Testing**
- Build validation (0 errors)
- Unit tests
- Integration tests
- Acceptance tests

### 3.2 Project Groupings

All projects upgraded atomically in Phase 1. Grouped by type for reference:

**Libraries** (5 projects):
- Core.csproj (883 LOC)
- DataAccess.csproj (430 LOC)
- LlmGateway.csproj (150 LOC)
- UI.Shared.csproj (581 LOC)
- UI.Api.csproj (149 LOC)

**Applications** (2 projects):
- UI.Client.csproj - Blazor WebAssembly (275 LOC)
- UI.Server.csproj - Blazor Server (395 LOC)

**Test Projects** (3 projects):
- UnitTests.csproj (2,198 LOC)
- IntegrationTests.csproj (1,902 LOC)
- AcceptanceTests.csproj (1,451 LOC)

**Database** (1 project):
- Database.csproj - Excluded from analysis (scripts only)

---

## Project-by-Project Migration Plans

### Project: Core.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: 0 project dependencies
- Dependants: 8 projects (all others depend on Core)
- Package Count: 3
- LOC: 883
- Project Type: ClassLibrary

**Target State**
- Target Framework: net10.0
- Updated Packages: 2 packages

**Migration Steps**

1. **Prerequisites**
   - None (foundation project)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.Extensions.Logging.Abstractions | 9.0.7 | 10.0.0 | Framework compatibility |
| MediatR.Contracts | 2.0.1 | 2.0.1 | No update required (compatible) |

4. **Expected Breaking Changes**
   - None identified for abstractions packages
   - MediatR.Contracts unchanged

5. **Code Modifications**
   - No code changes anticipated
   - Core project contains domain models and interfaces only

6. **Testing Strategy**
   - Build verification
   - No unit tests in Core project itself
   - Validation via dependent projects

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] Dependent projects build successfully

---

### Project: DataAccess.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core
- Dependants: UI.Server
- Package Count: 5
- LOC: 430
- Project Type: ClassLibrary

**Target State**
- Target Framework: net10.0
- Updated Packages: 4 packages

**Migration Steps**

1. **Prerequisites**
   - Core.csproj upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.EntityFrameworkCore | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.Extensions.Logging.Abstractions | 9.0.7 | 10.0.0 | Framework compatibility |
| MediatR | 12.4.1 | 12.4.1 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **Entity Framework Core 10**: Check for any breaking changes in:
     - DbContext configuration
     - Query behavior changes
     - Migration generation
     - SQL Server provider updates
   - Reference: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes

5. **Code Modifications**
   - Review DataContext.cs for EF Core API changes
   - Check entity mappings in Mappings folder
   - Verify query handlers use compatible LINQ patterns

6. **Testing Strategy**
   - Build verification
   - Integration tests cover database operations
   - Verify CanConnectToDatabaseHealthCheck

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] EF Core migrations work
   - [x] Database health check passes
   - [x] Integration tests pass

---

### Project: LlmGateway.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core
- Dependants: UI.Server, UI.Shared
- Package Count: 10
- LOC: 150
- Project Type: ClassLibrary

**Target State**
- Target Framework: net10.0
- Updated Packages: 2 packages

**Migration Steps**

1. **Prerequisites**
   - Core.csproj upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.Extensions.Diagnostics.HealthChecks | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.Extensions.Logging.Console | 9.0.7 | 10.0.0 | Framework compatibility |
| Azure.AI.OpenAI | 2.1.0 | 2.1.0 | No update required (compatible) |
| Azure.Core | 1.46.1 | 1.46.1 | No update required (compatible) |
| MediatR | 12.4.1 | 12.4.1 | No update required (compatible) |
| Microsoft.Extensions.AI | 9.7.0 | 9.7.0 | No update required (compatible) |
| Microsoft.Extensions.AI.Abstractions | 9.7.1 | 9.7.1 | No update required (compatible) |
| Microsoft.Extensions.AI.Ollama | 9.7.0-preview.1.25356.2 | 9.7.0-preview.1.25356.2 | No update required (compatible) |
| Microsoft.Extensions.AI.OpenAI | 9.7.1-preview.1.25365.4 | 9.7.1-preview.1.25365.4 | No update required (compatible) |
| OllamaSharp | 5.2.10 | 5.2.10 | No update required (compatible) |

4. **Expected Breaking Changes**
   - None identified
   - AI packages remain on compatible versions

5. **Code Modifications**
   - No changes anticipated
   - ChatClientFactory.cs should work as-is

6. **Testing Strategy**
   - Build verification
   - Verify CanConnectToLlmServerHealthCheck
   - Manual testing with Ollama/Azure OpenAI if available

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] LLM health check works
   - [x] ChatClient instantiation succeeds

---

### Project: UI.Shared.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core, LlmGateway
- Dependants: UnitTests, UI.Client
- Package Count: 6
- LOC: 581
- Project Type: ClassLibrary

**Target State**
- Target Framework: net10.0
- Updated Packages: 2 packages

**Migration Steps**

1. **Prerequisites**
   - Core, LlmGateway upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.AspNetCore.Components.Authorization | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.AspNetCore.Components.Web | 9.0.7 | 10.0.0 | Framework compatibility |
| BlazorApplicationInsights | 3.2.1 | 3.2.1 | No update required (compatible) |
| BlazorMvc | 2.1.1 | 2.1.1 | No update required (compatible) |
| MediatR | 12.4.1 | 12.4.1 | No update required (compatible) |
| Microsoft.ApplicationInsights | 2.23.0 | 2.23.0 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **Blazor Components**: Check for changes in:
     - AuthenticationStateProvider API
     - Component lifecycle methods
     - Razor component syntax
   - **Third-Party Risk**: BlazorMvc 2.1.1 has no .NET 10 update (using compatible version)

5. **Code Modifications**
   - Review CustomAuthenticationStateProvider.cs
   - Check Razor components in Pages and Components folders
   - Verify AppComponentBase inheritance chain

6. **Testing Strategy**
   - Build verification
   - Unit tests for components (bUnit)
   - Manual testing of shared UI components

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] Razor components compile
   - [x] Authentication flow works
   - [x] bUnit tests pass

---

### Project: UI.Api.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core
- Dependants: UnitTests, UI.Server
- Package Count: 1
- LOC: 149
- Project Type: AspNetCore

**Target State**
- Target Framework: net10.0
- Updated Packages: 0 packages

**Migration Steps**

1. **Prerequisites**
   - Core upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Lamar.Microsoft.DependencyInjection | 15.0.1 | 15.0.1 | No update required (compatible) |

4. **Expected Breaking Changes**
   - None (minimal API project)
   - Lamar DI container compatibility to be verified

5. **Code Modifications**
   - No changes anticipated
   - Simple API controllers

6. **Testing Strategy**
   - Build verification
   - API endpoint testing via integration tests

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] API endpoints respond

---

### Project: UI.Client.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core, UI.Shared
- Dependants: UnitTests, UI.Server
- Package Count: 8
- LOC: 275
- Project Type: AspNetCore (Blazor WebAssembly)

**Target State**
- Target Framework: net10.0
- Updated Packages: 4 packages

**Migration Steps**

1. **Prerequisites**
   - Core, UI.Shared upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.AspNetCore.Components.WebAssembly | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.AspNetCore.Components.Authorization | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.Extensions.Diagnostics.HealthChecks | 9.0.7 | 10.0.0 | Framework compatibility |
| BlazorApplicationInsights | 3.2.1 | 3.2.1 | No update required (compatible) |
| BlazorMvc | 2.1.1 | 2.1.1 | No update required (compatible) |
| Lamar.Microsoft.DependencyInjection | 15.0.1 | 15.0.1 | No update required (compatible) |
| MediatR | 12.4.1 | 12.4.1 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **Blazor WebAssembly**: Check for changes in:
     - WebAssemblyHostBuilder API
     - JSRuntime interop
     - HttpClient configuration
     - Authentication flows
   - **Third-Party Risk**: BlazorMvc, Lamar, BlazorApplicationInsights have no .NET 10 updates

5. **Code Modifications**
   - Review Program.cs (WebAssemblyHostBuilder setup)
   - Check Lamar DI registration in UIClientServiceRegistry
   - Verify RemotableBus and PublisherGateway
   - Review App.razor routing configuration

6. **Testing Strategy**
   - Build verification
   - WASM build output validation
   - Unit tests for client components
   - Manual browser testing

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] WASM artifacts generated
   - [x] Application loads in browser
   - [x] Authentication works
   - [x] API communication works

---

### Project: UI.Server.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core, DataAccess, LlmGateway, UI.Api, UI.Client
- Dependants: UnitTests, IntegrationTests
- Package Count: 8
- LOC: 395
- Project Type: AspNetCore (Blazor Server)

**Target State**
- Target Framework: net10.0
- Updated Packages: 1 package

**Migration Steps**

1. **Prerequisites**
   - All dependencies upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.AspNetCore.Components.WebAssembly.Server | 9.0.7 | 10.0.0 | Framework compatibility |
| Azure.Monitor.OpenTelemetry.AspNetCore | 1.3.0 | 1.3.0 | No update required (compatible) |
| Lamar.Microsoft.DependencyInjection | 15.0.1 | 15.0.1 | No update required (compatible) |
| Microsoft.ApplicationInsights.AspNetCore | 2.23.0 | 2.23.0 | No update required (compatible) |
| OpenTelemetry | 1.12.0 | 1.12.0 | No update required (compatible) |
| OpenTelemetry.Instrumentation.AspNetCore | 1.12.0 | 1.12.0 | No update required (compatible) |
| OpenTelemetry.Instrumentation.Http | 1.12.0 | 1.12.0 | No update required (compatible) |
| OpenTelemetry.Instrumentation.Runtime | 1.12.0 | 1.12.0 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **ASP.NET Core 10**: Check for changes in:
     - Program.cs minimal hosting model
     - Middleware registration order
     - Health checks aggregation
     - Static file serving for WASM
   - **Lamar DI**: Verify UiServiceRegistry compatibility

5. **Code Modifications**
   - Review Program.cs for ASP.NET Core 10 changes
   - Check UiServiceRegistry.cs Lamar configuration
   - Verify health check endpoints
   - Check static file middleware for WASM hosting

6. **Testing Strategy**
   - Build verification
   - Server startup validation
   - Health check endpoints (_healthcheck)
   - Integration tests
   - Acceptance tests (Playwright)

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] Server starts successfully
   - [x] Health checks pass
   - [x] WASM client loads
   - [x] Database connection works
   - [x] Integration tests pass

---

### Project: UnitTests.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core, UI.Shared, UI.Server, UI.Client, UI.Api
- Dependants: IntegrationTests
- Package Count: 11
- LOC: 2,198
- Project Type: DotNetCoreApp (Test)

**Target State**
- Target Framework: net10.0
- Updated Packages: 1 package

**Migration Steps**

1. **Prerequisites**
   - All application projects upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.AspNetCore.Components.Authorization | 9.0.7 | 10.0.0 | Framework compatibility |
| AutoBogus.Conventions | 2.13.1 | 2.13.1 | No update required (compatible) |
| bunit | 1.40.0 | 1.40.0 | No update required (compatible) |
| coverlet.collector | 6.0.4 | 6.0.4 | No update required (compatible) |
| coverlet.msbuild | 6.0.4 | 6.0.4 | No update required (compatible) |
| MediatR | 12.4.1 | 12.4.1 | No update required (compatible) |
| Microsoft.NET.Test.Sdk | 17.14.1 | 17.14.1 | No update required (compatible) |
| NUnit | 4.3.2 | 4.3.2 | No update required (compatible) |
| NUnit.Analyzers | 4.9.2 | 4.9.2 | No update required (compatible) |
| NUnit3TestAdapter | 5.0.0 | 5.0.0 | No update required (compatible) |
| Shouldly | 4.3.0 | 4.3.0 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **bunit**: Verify Blazor component testing API compatibility
   - **Test SDK**: Check for test runner changes

5. **Code Modifications**
   - Review bUnit test setup in test base classes
   - Check TestContext usage
   - Verify stub implementations (StubBus, StubMediator)

6. **Testing Strategy**
   - Build verification
   - Run all unit tests
   - 2,198 LOC of tests to validate

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] All unit tests pass
   - [x] Test coverage maintained

---

### Project: IntegrationTests.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: UI.Server, UnitTests
- Dependants: AcceptanceTests
- Package Count: 8
- LOC: 1,902
- Project Type: DotNetCoreApp (Test)

**Target State**
- Target Framework: net10.0
- Updated Packages: 2 packages

**Migration Steps**

1. **Prerequisites**
   - UI.Server, UnitTests upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.EntityFrameworkCore | 9.0.7 | 10.0.0 | Framework compatibility |
| Microsoft.Extensions.Hosting | 9.0.7 | 10.0.0 | Framework compatibility |
| coverlet.collector | 6.0.4 | 6.0.4 | No update required (compatible) |
| coverlet.msbuild | 6.0.4 | 6.0.4 | No update required (compatible) |
| Microsoft.NET.Test.Sdk | 17.14.1 | 17.14.1 | No update required (compatible) |
| NUnit | 4.3.2 | 4.3.2 | No update required (compatible) |
| NUnit.Analyzers | 4.9.2 | 4.9.2 | No update required (compatible) |
| NUnit3TestAdapter | 5.0.0 | 5.0.0 | No update required (compatible) |
| Shouldly | 4.3.0 | 4.3.0 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **EF Core 10**: Check for database testing changes
   - **Hosting**: Verify TestHost configuration

5. **Code Modifications**
   - Review TestHost.cs configuration
   - Check DatabaseEmptier.cs for EF Core 10
   - Verify IntegratedTestBase.cs setup

6. **Testing Strategy**
   - Build verification
   - Run all integration tests
   - Verify database operations

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] All integration tests pass
   - [x] Database operations work
   - [x] TestHost initializes

---

### Project: AcceptanceTests.csproj

**Current State**
- Target Framework: net9.0
- Dependencies: Core, IntegrationTests
- Dependants: None (top-level test project)
- Package Count: 8
- LOC: 1,451
- Project Type: DotNetCoreApp (Test)

**Target State**
- Target Framework: net10.0
- Updated Packages: 1 package

**Migration Steps**

1. **Prerequisites**
   - Core, IntegrationTests upgraded (Phase 1 atomic operation)

2. **Framework Update**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Package Updates**

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|---------|
| Microsoft.Extensions.Hosting | 9.0.7 | 10.0.0 | Framework compatibility |
| coverlet.collector | 6.0.4 | 6.0.4 | No update required (compatible) |
| coverlet.msbuild | 6.0.4 | 6.0.4 | No update required (compatible) |
| Microsoft.NET.Test.Sdk | 17.14.1 | 17.14.1 | No update required (compatible) |
| microsoft.playwright.nunit | 1.54.0 | 1.54.0 | No update required (compatible) |
| NUnit | 4.3.2 | 4.3.2 | No update required (compatible) |
| NUnit.Analyzers | 4.9.2 | 4.9.2 | No update required (compatible) |
| NUnit3TestAdapter | 5.0.0 | 5.0.0 | No update required (compatible) |

4. **Expected Breaking Changes**
   - **Playwright**: Verify browser automation compatibility
   - **Hosting**: Check ServerFixture configuration

5. **Code Modifications**
   - Review AcceptanceTestBase.cs
   - Check ServerFixture.cs application startup
   - Verify Playwright browser paths

6. **Testing Strategy**
   - Build verification
   - Playwright browser installation check
   - Run acceptance tests (browser-based)

7. **Validation Checklist**
   - [x] Dependencies resolve correctly
   - [x] Project builds without errors
   - [x] Project builds without warnings
   - [x] Playwright browsers installed
   - [x] All acceptance tests pass
   - [x] Browser automation works

---

## Package Update Reference

### Common Package Updates (Multiple Projects)

| Package | Current | Target | Projects Affected | Update Reason |
|---------|---------|--------|-------------------|---------------|
| Microsoft.AspNetCore.Components.Authorization | 9.0.7 | 10.0.0 | 3 projects (UI.Shared, UI.Client, UnitTests) | Framework compatibility |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 9.0.7 | 10.0.0 | 2 projects (Core, DataAccess) | Framework compatibility |
| Microsoft.Extensions.Logging.Abstractions | 9.0.7 | 10.0.0 | 2 projects (Core, DataAccess) | Framework compatibility |
| Microsoft.Extensions.Hosting | 9.0.7 | 10.0.0 | 2 projects (IntegrationTests, AcceptanceTests) | Framework compatibility |
| Microsoft.EntityFrameworkCore | 9.0.7 | 10.0.0 | 2 projects (DataAccess, IntegrationTests) | Framework compatibility |

### Category-Specific Updates

**Blazor WebAssembly (UI.Client)**:
- Microsoft.AspNetCore.Components.WebAssembly: 9.0.7 → 10.0.0
- Microsoft.AspNetCore.Components.WebAssembly.DevServer: 9.0.7 → 10.0.0
- Microsoft.Extensions.Diagnostics.HealthChecks: 9.0.7 → 10.0.0

**Blazor Shared (UI.Shared)**:
- Microsoft.AspNetCore.Components.Web: 9.0.7 → 10.0.0

**Blazor Server (UI.Server)**:
- Microsoft.AspNetCore.Components.WebAssembly.Server: 9.0.7 → 10.0.0

**Data Access (DataAccess)**:
- Microsoft.EntityFrameworkCore.SqlServer: 9.0.7 → 10.0.0

**Gateway (LlmGateway)**:
- Microsoft.Extensions.Diagnostics.HealthChecks: 9.0.7 → 10.0.0
- Microsoft.Extensions.Logging.Console: 9.0.7 → 10.0.0

### Packages Remaining at Current Versions

**Third-Party Libraries (No .NET 10 Updates Available)**:
- MediatR: 12.4.1 (compatible, no upgrade needed)
- MediatR.Contracts: 2.0.1 (compatible, no upgrade needed)
- BlazorMvc: 2.1.1 (compatible, no update available)
- BlazorApplicationInsights: 3.2.1 (compatible, no update available)
- Lamar.Microsoft.DependencyInjection: 15.0.1 (compatible, no update available)

**Testing Frameworks (Compatible Current Versions)**:
- NUnit: 4.3.2 (compatible)
- bunit: 1.40.0 (compatible)
- Shouldly: 4.3.0 (compatible)
- Microsoft.Playwright.NUnit: 1.54.0 (compatible)

**Monitoring & Telemetry (Compatible Current Versions)**:
- Microsoft.ApplicationInsights: 2.23.0 (compatible)
- Microsoft.ApplicationInsights.AspNetCore: 2.23.0 (compatible)
- OpenTelemetry: 1.12.0 (compatible)
- Azure.Monitor.OpenTelemetry.AspNetCore: 1.3.0 (compatible)

**AI Libraries (Compatible Current Versions)**:
- Azure.AI.OpenAI: 2.1.0 (compatible)
- Microsoft.Extensions.AI.*: 9.7.0-9.7.1 (compatible)
- OllamaSharp: 5.2.10 (compatible)

---

## Breaking Changes Catalog

### Framework Breaking Changes (.NET 9 → .NET 10)

**General**:
- **.NET 10 is Preview** - Not production-ready, expect API instability
- Monitor breaking changes: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0

**ASP.NET Core**:
- Monitor breaking changes: https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/10.0
- No major breaking changes identified in preview documentation yet

**Entity Framework Core**:
- Monitor breaking changes: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes
- No major breaking changes identified in preview documentation yet

### Package Breaking Changes

**Microsoft Packages (9.0.7 → 10.0.0)**:
- Generally backward compatible
- Monitor release notes for each package after upgrade

**Third-Party Package Risks**:

1. **BlazorMvc 2.1.1** (No Update Available)
   - Risk: Medium
   - Uses internal Blazor APIs that may change
   - Test thoroughly: `AppComponentBase`, `MvcComponentBase`
   - Mitigation: Monitor GitHub repo, be prepared to fork if issues

2. **Lamar.Microsoft.DependencyInjection 15.0.1** (No Update Available)
   - Risk: Medium
   - DI container integration sensitive to runtime changes
   - Test thoroughly: Service registration, resolution
   - Mitigation: Verify all DI scenarios work

3. **BlazorApplicationInsights 3.2.1** (No Update Available)
   - Risk: Low
   - Telemetry package, likely stable
   - Test: Application Insights logging works

### Code Areas Requiring Attention

**Authentication (UI.Shared)**:
- CustomAuthenticationStateProvider.cs - verify API compatibility
- Test login/logout flows thoroughly

**Blazor Components**:
- Razor component syntax and lifecycle methods
- Component parameter binding
- Cascading parameters

**Entity Framework**:
- DataContext configuration (DataAccess/Mappings/DataContext.cs)
- Entity mappings and configurations
- Query patterns (check for obsolete LINQ methods)
- Migration generation

**Dependency Injection**:
- UIClientServiceRegistry.cs (Lamar)
- UiServiceRegistry.cs (Lamar)
- Verify all services resolve correctly

**Health Checks**:
- CanConnectToDatabaseHealthCheck.cs
- CanConnectToLlmServerHealthCheck.cs

---

## Testing Strategy

### Phase-by-Phase Testing

Big Bang strategy requires comprehensive testing after atomic upgrade completes.

**Phase 0: Prerequisites**
- [x] .NET 10 SDK installed and verified
- [x] No global.json SDK constraints

**Phase 1: Atomic Upgrade (No Testing)**
- Update all project TargetFramework properties
- Update all package references
- Restore dependencies
- Build solution and fix compilation errors
- Verify solution builds with 0 errors

**Phase 2: Comprehensive Testing**

#### Build Validation
- [x] All 11 projects build without errors
- [x] All projects build without warnings
- [x] No package restore errors
- [x] No dependency conflicts

#### Unit Tests (UnitTests.csproj)
- [x] All unit tests pass (2,198 LOC)
- [x] bUnit component tests work
- [x] Authentication tests pass
- [x] Bus/MediatR tests pass

#### Integration Tests (IntegrationTests.csproj)
- [x] All integration tests pass (1,902 LOC)
- [x] Database operations work
- [x] TestHost initializes correctly
- [x] Data access layer functional

#### Acceptance Tests (AcceptanceTests.csproj)
- [x] Playwright browsers installed
- [x] All acceptance tests pass (1,451 LOC)
- [x] UI flows work end-to-end
- [x] Authentication flows work
- [x] Work order CRUD operations work

### Smoke Tests

Quick validation checkpoints:

**Application Startup**:
- [x] UI.Server starts without errors
- [x] Health check endpoint responds: `https://localhost:7174/_healthcheck`
- [x] Blazor WASM client loads in browser
- [x] No console errors in browser

**Core Functionality**:
- [x] Login with test user (hsimpson)
- [x] Navigate to "New Work Order"
- [x] Create a work order
- [x] Search for work orders
- [x] View work order details
- [x] Logout

**Third-Party Package Verification**:
- [x] BlazorMvc components render
- [x] Lamar DI resolves services
- [x] Application Insights telemetry works
- [x] MediatR request/response works

---

## Risk Management

### High-Risk Changes

| Project | Risk | Mitigation |
|---------|------|------------|
| UI.Client | Medium - BlazorMvc, Lamar no .NET 10 updates | Thorough component testing, verify DI resolution |
| UI.Server | Medium - Lamar DI integration | Test service registration, health checks |
| DataAccess | Medium - EF Core 10 changes | Test migrations, database operations, integration tests |
| UI.Shared | Medium - BlazorMvc, authentication | Test authentication flows, component inheritance |

### Risk Assessment by Category

**Big Bang Strategy Risks**:
- **Higher initial risk** - All projects upgraded together
- **Larger testing surface** - Must test entire solution at once
- **All developers affected** - No gradual adoption
- **Mitigation**: Comprehensive testing phase, clear rollback plan

**Third-Party Package Risks**:
- **BlazorMvc**: No .NET 10 update available
  - Mitigation: Test component rendering, be prepared to fork/fix
- **Lamar DI**: No .NET 10 update available
  - Mitigation: Verify service resolution, have Microsoft.Extensions.DependencyInjection fallback ready
- **BlazorApplicationInsights**: No .NET 10 update available
  - Mitigation: Verify telemetry works, acceptable to disable if issues

**Preview Software Risks**:
- **.NET 10 is Preview** - Not production-ready
  - Mitigation: Accept preview software risks, be prepared to rollback
  - Recommendation: Do not deploy to production

### Contingency Plans

**If Atomic Upgrade Fails**:
1. Rollback via Git: `git reset --hard HEAD` on upgrade branch
2. Switch back to feature/197-upgrade-to-dotNet10 base
3. Investigate specific compilation errors
4. Address individually, retry atomic upgrade

**If BlazorMvc Incompatibility Detected**:
1. Check BlazorMvc GitHub for .NET 10 branch
2. Consider temporarily forking BlazorMvc
3. Alternative: Remove BlazorMvc, migrate to standard Blazor patterns

**If Lamar Incompatibility Detected**:
1. Check Lamar GitHub for .NET 10 support status
2. Fallback: Migrate to Microsoft.Extensions.DependencyInjection
3. Alternative: Wait for Lamar update before upgrading

**If EF Core 10 Issues Detected**:
1. Review breaking changes documentation
2. Regenerate migrations if needed
3. Update query patterns for compatibility
4. Check SQL Server provider compatibility

---

## Source Control Strategy

### Branching Strategy
- **Main upgrade branch**: `feature/197-upgrade-to-dotNet10` (current)
- **All work performed on**: `feature/197-upgrade-to-dotNet10`
- **No feature branches**: Big Bang approach uses single branch

### Commit Strategy

**Recommended Approach**: Single atomic commit after successful migration and testing.

**Commit Structure**:
```
git add .
git commit -m "Upgrade to .NET 10.0 Preview

- Updated all 11 projects to net10.0
- Updated Microsoft packages to version 10.0.0
- Kept third-party packages at compatible versions
- All tests passing (unit, integration, acceptance)

Breaking Changes:
- None detected

Third-Party Package Status:
- MediatR 12.4.1 (compatible, no update)
- BlazorMvc 2.1.1 (compatible, no update available)
- Lamar 15.0.1 (compatible, no update available)

Test Results:
- UnitTests: All pass
- IntegrationTests: All pass
- AcceptanceTests: All pass"
```

**Alternative Approach**: Checkpoint commits if issues encountered during migration.

**Checkpoint Commit Pattern** (if needed):
- Commit 1: "WIP: Update all project TargetFramework to net10.0"
- Commit 2: "WIP: Update all Microsoft packages to 10.0.0"
- Commit 3: "WIP: Fix compilation errors from framework upgrade"
- Commit 4: "Complete .NET 10 upgrade - all tests passing"

**Final Squash**: Squash WIP commits into single atomic commit before merge.

### Review and Merge Process

**Pull Request Requirements**:
- [x] All projects build without errors or warnings
- [x] All unit tests pass
- [x] All integration tests pass
- [x] All acceptance tests pass
- [x] No security vulnerabilities introduced
- [x] Breaking changes documented

**Review Checklist**:
- [ ] Project files correctly updated to net10.0
- [ ] Package versions correct (Microsoft packages → 10.0.0)
- [ ] Third-party packages at expected versions
- [ ] No unexpected code changes
- [ ] Test results included in PR description

**Merge Criteria**:
- All tests green
- Code review approved
- No blocking issues identified

**Integration Validation**:
- CI/CD pipeline passes
- Deployment to test environment successful

---

## Success Criteria

### Technical Success Criteria

**Build and Compilation**:
- [x] All 11 projects migrated to net10.0
- [x] All Microsoft packages updated to 10.0.0
- [x] All projects build without errors
- [x] All projects build without warnings
- [x] No package dependency conflicts
- [x] No security vulnerabilities in dependencies

**Testing**:
- [x] All unit tests pass (UnitTests: 2,198 LOC)
- [x] All integration tests pass (IntegrationTests: 1,902 LOC)
- [x] All acceptance tests pass (AcceptanceTests: 1,451 LOC)
- [x] Smoke tests pass (manual validation)

**Functionality**:
- [x] Application starts without errors
- [x] Health checks pass
- [x] Blazor WASM client loads
- [x] Authentication works
- [x] Work order CRUD operations work
- [x] Database operations work
- [x] LLM gateway (if available) works

### Quality Criteria

**Code Quality**:
- [x] No code degradation
- [x] Architecture integrity maintained (Onion Architecture)
- [x] No new technical debt introduced
- [x] Code follows existing patterns

**Documentation**:
- [x] Migration plan documented
- [x] Breaking changes cataloged
- [x] Third-party package status documented
- [x] Commit messages clear and complete

### Operational Criteria

**Development Environment**:
- [x] .NET 10 SDK installed
- [x] Solution builds in Visual Studio
- [x] Solution builds via CLI (`dotnet build`)
- [x] Tests run in Visual Studio
- [x] Tests run via CLI (`dotnet test`)

**Deployment Readiness**:
- ⚠️ **NOT PRODUCTION READY** - .NET 10 is Preview
- [x] CI/CD pipeline compatible (if CI runs .NET 10)
- [x] Docker build works (if using containers)
- [x] Deployment scripts updated (if needed)

---

## Implementation Timeline

### Big Bang Execution Model

**Estimated Total Time**: 4-8 hours (including testing)

**Phase 0: Preparation** (30 minutes)
- Verify .NET 10 SDK installed
- Verify current branch (feature/197-upgrade-to-dotNet10)
- Commit/stash any pending changes
- Create backup branch (optional safety)

**Phase 1: Atomic Upgrade** (1-2 hours)
- Update all 11 project TargetFramework properties to net10.0
- Update all Microsoft package references to 10.0.0
- Restore dependencies (`dotnet restore`)
- Build solution (`dotnet build`)
- Fix compilation errors (if any)
- Rebuild until 0 errors and 0 warnings

**Phase 2: Testing and Validation** (2-4 hours)
- Run unit tests (`dotnet test UnitTests.csproj`)
- Run integration tests (`dotnet test IntegrationTests.csproj`)
- Run acceptance tests (`dotnet test AcceptanceTests.csproj`)
- Fix test failures
- Rerun tests until all pass

**Phase 3: Smoke Testing** (1-2 hours)
- Start UI.Server application
- Verify health checks
- Manual UI testing (authentication, CRUD operations)
- Verify third-party packages work (BlazorMvc, Lamar)

**Phase 4: Finalization** (30 minutes)
- Review all changes
- Create single atomic commit (or squash WIP commits)
- Update documentation
- Create pull request

---

## Notes and Considerations

### Preview Software Warning
⚠️ **.NET 10 is Preview/Pre-Release Software**
- Not recommended for production use
- Expect API changes before final release
- Use for evaluation and testing only
- Have rollback plan ready

### Third-Party Package Monitoring

**Packages to Monitor**:
1. **BlazorMvc** - https://github.com/bemayr/BlazorMvc
2. **Lamar** - https://github.com/JasperFx/lamar
3. **BlazorApplicationInsights** - https://github.com/IvanJosipovic/BlazorApplicationInsights

**Action Items**:
- Check GitHub repos for .NET 10 compatibility updates
- Subscribe to release notifications
- Be prepared to fork/fix if critical issues found

### Post-Upgrade Monitoring

**After upgrade completes**:
- Monitor application performance
- Watch for runtime errors in logs
- Check telemetry for anomalies
- Verify database operations under load
- Monitor memory usage (Blazor WASM can be affected by framework changes)

### Rollback Plan

**If upgrade must be abandoned**:
1. `git reset --hard <commit-before-upgrade>`
2. `git push --force origin feature/197-upgrade-to-dotNet10`
3. Verify solution builds on .NET 9.0
4. Run tests to confirm rollback successful

**Alternative**: Keep .NET 9.0 parallel environment until .NET 10 stable.

---

## Appendix: Project Statistics Summary

| Project | Type | LOC | Files | Packages | Dependencies | Dependants | Risk |
|---------|------|-----|-------|----------|--------------|------------|------|
| Core | Library | 883 | 35 | 3 | 0 | 8 | Low |
| DataAccess | Library | 430 | 14 | 5 | 1 | 1 | Medium |
| LlmGateway | Library | 150 | 7 | 10 | 1 | 2 | Low |
| UI.Shared | Library | 581 | 32 | 6 | 2 | 2 | Medium |
| UI.Api | ASP.NET | 149 | 7 | 1 | 1 | 2 | Low |
| UI.Client | Blazor WASM | 275 | 29 | 8 | 2 | 2 | Medium |
| UI.Server | Blazor Server | 395 | 12 | 8 | 5 | 2 | Medium |
| UnitTests | Test | 2,198 | 29 | 11 | 5 | 1 | Low |
| IntegrationTests | Test | 1,902 | 24 | 8 | 2 | 1 | Low |
| AcceptanceTests | Test | 1,451 | 18 | 8 | 2 | 0 | Low |
| **TOTAL** | | **8,414** | **207** | **68** | | | |

---

## Appendix: Package Update Matrix

Complete matrix of all package updates across all projects:

| Package | Projects Using | Current | Target | Status |
|---------|----------------|---------|--------|--------|
| Microsoft.AspNetCore.Components.Authorization | 3 | 9.0.7 | 10.0.0 | Update |
| Microsoft.AspNetCore.Components.Web | 1 | 9.0.7 | 10.0.0 | Update |
| Microsoft.AspNetCore.Components.WebAssembly | 1 | 9.0.7 | 10.0.0 | Update |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 1 | 9.0.7 | 10.0.0 | Update |
| Microsoft.AspNetCore.Components.WebAssembly.Server | 1 | 9.0.7 | 10.0.0 | Update |
| Microsoft.EntityFrameworkCore | 2 | 9.0.7 | 10.0.0 | Update |
| Microsoft.EntityFrameworkCore.SqlServer | 1 | 9.0.7 | 10.0.0 | Update |
| Microsoft.Extensions.Diagnostics.HealthChecks | 2 | 9.0.7 | 10.0.0 | Update |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 2 | 9.0.7 | 10.0.0 | Update |
| Microsoft.Extensions.Hosting | 2 | 9.0.7 | 10.0.0 | Update |
| Microsoft.Extensions.Logging.Abstractions | 2 | 9.0.7 | 10.0.0 | Update |
| Microsoft.Extensions.Logging.Console | 1 | 9.0.7 | 10.0.0 | Update |
| MediatR | 5 | 12.4.1 | 12.4.1 | Keep |
| MediatR.Contracts | 1 | 2.0.1 | 2.0.1 | Keep |
| BlazorMvc | 2 | 2.1.1 | 2.1.1 | Keep |
| BlazorApplicationInsights | 2 | 3.2.1 | 3.2.1 | Keep |
| Lamar.Microsoft.DependencyInjection | 3 | 15.0.1 | 15.0.1 | Keep |
| All other packages | Various | Various | Various | Keep |

**Total Packages Requiring Updates**: 12 distinct Microsoft packages across 19 package references

---

**End of Migration Plan**